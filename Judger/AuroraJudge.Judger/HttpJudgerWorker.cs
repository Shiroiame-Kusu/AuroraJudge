using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AuroraJudge.Judger.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AuroraJudge.Judger;

/// <summary>
/// 基于 HTTP API 拉取任务的判题机后台服务
/// </summary>
public class HttpJudgerWorker : BackgroundService
{
    private readonly ILogger<HttpJudgerWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly JudgeService _judgeService;
    private readonly HttpClient _httpClient;
    
    private Guid _judgerId;
    private string _secret;
    private readonly string _judgerName;
    private readonly int _maxConcurrentTasks;
    private readonly string _backendUrl;
    private readonly int _pollIntervalMs;
    
    private int _currentTasks;
    private bool _isConnected;

    private static int GetInt(IConfiguration configuration, string key, int defaultValue)
    {
        var raw = configuration[key];
        return int.TryParse(raw, out var value) ? value : defaultValue;
    }
    
    public HttpJudgerWorker(ILogger<HttpJudgerWorker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _judgerName = _configuration["Judger:Name"] ?? $"judger-{Environment.MachineName}";
        _maxConcurrentTasks = GetInt(_configuration, "Judger:MaxConcurrentTasks", 4);
        _backendUrl = _configuration["Judger:BackendUrl"] ?? "http://localhost:5000";
        _pollIntervalMs = GetInt(_configuration, "Judger:PollIntervalMs", 1000);
        
        // 从配置读取凭证
        var judgerIdStr = _configuration["Judger:JudgerId"];
        _judgerId = string.IsNullOrEmpty(judgerIdStr) ? Guid.Empty : Guid.Parse(judgerIdStr);
        _secret = _configuration["Judger:Secret"] ?? string.Empty;
        
        var workDir = _configuration["Judger:WorkDir"] ?? "/tmp/aurora-judge";
        _judgeService = new JudgeService(logger, workDir);
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_backendUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("==============================================");
        _logger.LogInformation("   AuroraJudge Judger (HTTP 模式)");
        _logger.LogInformation("==============================================");
        _logger.LogInformation("  名称: {Name}", _judgerName);
        _logger.LogInformation("  并发数: {MaxTasks}", _maxConcurrentTasks);
        _logger.LogInformation("  后端地址: {Url}", _backendUrl);
        _logger.LogInformation("  JudgerId: {JudgerId}", _judgerId);
        _logger.LogInformation("  Secret: {SecretStatus}", string.IsNullOrWhiteSpace(_secret) ? "<empty>" : "<set>");
        _logger.LogInformation("==============================================");
        
        // 连接到后端
        while (!stoppingToken.IsCancellationRequested && !_isConnected)
        {
            await ConnectToBackendAsync(stoppingToken);
            if (!_isConnected)
            {
                _logger.LogWarning("连接后端失败，5秒后重试...");
                await Task.Delay(5000, stoppingToken);
            }
        }
        
        if (!_isConnected)
        {
            _logger.LogError("无法连接到后端，Judger 退出");
            return;
        }
        
        // 启动心跳任务
        _ = SendHeartbeatAsync(stoppingToken);
        
        // 开始轮询获取任务
        await PollTasksAsync(stoppingToken);
    }
    
    private async Task ConnectToBackendAsync(CancellationToken ct)
    {
        try
        {
            if (_judgerId == Guid.Empty || string.IsNullOrWhiteSpace(_secret))
            {
                _logger.LogWarning("配置中的 JudgerId/Secret 为空或无效：JudgerId={JudgerId}, Secret={SecretStatus}", _judgerId, string.IsNullOrWhiteSpace(_secret) ? "<empty>" : "<set>");
            }

            var request = new JudgerConnectRequest
            {
                JudgerId = _judgerId,
                Secret = _secret
            };
            
            var response = await _httpClient.PostAsJsonAsync(
                "/api/judger/connect",
                request,
                JudgerJsonContext.Default.JudgerConnectRequest,
                ct);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync(
                    JudgerJsonContext.Default.JudgerConnectResponse,
                    ct);
                if (result != null)
                {
                    _judgerId = result.JudgerId;
                    _isConnected = true;
                    _logger.LogInformation("✓ 已连接到后端: {Message}", result.Message);
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("连接失败: {StatusCode} - {Error}", response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "连接后端异常");
        }
    }
    
    private async Task PollTasksAsync(CancellationToken ct)
    {
        _logger.LogInformation("开始轮询任务...");
        
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // 检查是否可以接受新任务
                if (_currentTasks >= _maxConcurrentTasks)
                {
                    await Task.Delay(_pollIntervalMs, ct);
                    continue;
                }
                
                // 尝试获取任务
                var task = await FetchTaskAsync(ct);
                if (task != null)
                {
                    // 异步处理任务
                    _ = ProcessTaskAsync(task, ct);
                }
                else
                {
                    // 无任务时等待
                    await Task.Delay(_pollIntervalMs, ct);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "轮询任务异常");
                await Task.Delay(5000, ct);
            }
        }
    }
    
    private async Task<JudgeTaskResponse?> FetchTaskAsync(CancellationToken ct)
    {
        try
        {
            var request = new JudgerFetchRequest
            {
                JudgerId = _judgerId,
                Secret = _secret
            };
            
            var response = await _httpClient.PostAsJsonAsync(
                "/api/judger/fetch",
                request,
                JudgerJsonContext.Default.JudgerFetchRequest,
                ct);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync(
                    JudgerJsonContext.Default.JudgerFetchResponse,
                    ct);
                if (result != null && result.HasTask && result.Task != null)
                {
                    return result.Task;
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("认证失败，尝试重新连接...");
                _isConnected = false;
                await ConnectToBackendAsync(ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取任务失败");
        }
        
        return null;
    }
    
    private async Task ProcessTaskAsync(JudgeTaskResponse task, CancellationToken ct)
    {
        Interlocked.Increment(ref _currentTasks);
        
        try
        {
            _logger.LogInformation("开始评测: {SubmissionId}", task.SubmissionId);
            
            // 转换为内部格式
            var judgeTask = new JudgeTask
            {
                SubmissionId = task.SubmissionId,
                Code = task.Code,
                Language = task.Language,
                TimeLimit = task.TimeLimit,
                MemoryLimit = task.MemoryLimit,
                TestCases = task.TestCases.Select(tc => new TestCaseData
                {
                    Order = tc.Order,
                    InputPath = tc.InputPath,
                    OutputPath = tc.OutputPath,
                    Score = tc.Score
                }).ToList()
            };
            
            // 执行评测
            var result = await _judgeService.JudgeAsync(judgeTask, ct);
            
            // 上报结果
            await ReportResultAsync(result, ct);
            
            _logger.LogInformation("评测完成: {SubmissionId} - {Status}", task.SubmissionId, result.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "评测失败: {SubmissionId}", task.SubmissionId);
            
            // 上报错误结果
            var errorResult = new JudgeResponse
            {
                SubmissionId = task.SubmissionId,
                Status = JudgeStatus.SystemError,
                CompileInfo = ex.Message
            };
            await ReportResultAsync(errorResult, ct);
        }
        finally
        {
            Interlocked.Decrement(ref _currentTasks);
        }
    }
    
    private async Task ReportResultAsync(JudgeResponse result, CancellationToken ct)
    {
        try
        {
            var request = new JudgerReportRequest
            {
                JudgerId = _judgerId,
                Secret = _secret,
                SubmissionId = result.SubmissionId,
                Status = result.Status.ToString(),
                Score = result.Score,
                TimeUsed = result.Time,
                MemoryUsed = result.Memory,
                CompileMessage = result.CompileInfo,
                JudgeMessage = null,
                TestResults = result.Results?.Select(tr => new TestCaseResultRequest
                {
                    Order = tr.Order,
                    Status = tr.Status.ToString(),
                    TimeUsed = tr.Time,
                    MemoryUsed = tr.Memory,
                    Score = tr.Score
                }).ToList()
            };
            
            var response = await _httpClient.PostAsJsonAsync(
                "/api/judger/report",
                request,
                JudgerJsonContext.Default.JudgerReportRequest,
                ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("上报结果失败: {StatusCode} - {Error}", response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上报结果异常");
        }
    }
    
    private async Task SendHeartbeatAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var request = new JudgerHeartbeatRequest
                {
                    JudgerId = _judgerId,
                    Secret = _secret
                };
                
                var response = await _httpClient.PostAsJsonAsync(
                    "/api/judger/heartbeat",
                    request,
                    JudgerJsonContext.Default.JudgerHeartbeatRequest,
                    ct);
                
                if (!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("心跳认证失败，尝试重新连接...");
                    _isConnected = false;
                    await ConnectToBackendAsync(ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "发送心跳失败");
            }
            
            await Task.Delay(30000, ct);
        }
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Judger {Name} 正在关闭...", _judgerName);
        _httpClient.Dispose();
        await base.StopAsync(cancellationToken);
    }
}

#region API DTOs

public class JudgerConnectRequest
{
    public Guid JudgerId { get; set; }
    public string Secret { get; set; } = string.Empty;
}

public class JudgerConnectResponse
{
    public Guid JudgerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MaxConcurrentTasks { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class JudgerHeartbeatRequest
{
    public Guid JudgerId { get; set; }
    public string Secret { get; set; } = string.Empty;
}

public class JudgerFetchRequest
{
    public Guid JudgerId { get; set; }
    public string Secret { get; set; } = string.Empty;
}

public class JudgerFetchResponse
{
    public bool HasTask { get; set; }
    public JudgeTaskResponse? Task { get; set; }
}

public class JudgeTaskResponse
{
    public Guid TaskId { get; set; }
    public Guid SubmissionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int TimeLimit { get; set; }
    public int MemoryLimit { get; set; }
    public string JudgeMode { get; set; } = string.Empty;
    public string? SpecialJudgeCode { get; set; }
    public List<JudgeTestCaseResponse> TestCases { get; set; } = new();
}

public class JudgeTestCaseResponse
{
    public int Order { get; set; }
    public string InputPath { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public int Score { get; set; }
}

public class JudgerReportRequest
{
    public Guid JudgerId { get; set; }
    public string Secret { get; set; } = string.Empty;
    public Guid SubmissionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? Score { get; set; }
    public int? TimeUsed { get; set; }
    public int? MemoryUsed { get; set; }
    public string? CompileMessage { get; set; }
    public string? JudgeMessage { get; set; }
    public List<TestCaseResultRequest>? TestResults { get; set; }
}

public class TestCaseResultRequest
{
    public int Order { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TimeUsed { get; set; }
    public int MemoryUsed { get; set; }
    public int Score { get; set; }
}

#endregion

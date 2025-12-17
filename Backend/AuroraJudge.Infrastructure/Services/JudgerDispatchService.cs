using System.Collections.Concurrent;
using AuroraJudge.Application.Services;
using AuroraJudge.Domain.Entities;
using AuroraJudge.Domain.Enums;
using AuroraJudge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AuroraJudge.Infrastructure.Services;

/// <summary>
/// Judger 节点缓存（内部存储，用于调度）
/// </summary>
internal class JudgerNodeCache
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SecretHash { get; set; } = string.Empty;
    public int MaxConcurrentTasks { get; set; } = 4;
    public int CurrentTasks { get; set; }
    public string Status { get; set; } = "Offline";
    public DateTime? LastHeartbeat { get; set; }
    public List<string> SupportedLanguages { get; set; } = new();
}

/// <summary>
/// 评测任务（内部队列）
/// </summary>
internal class JudgeTaskInternal
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid? AssignedJudgerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AssignedAt { get; set; }
    public int RetryCount { get; set; }
    
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int TimeLimit { get; set; }
    public int MemoryLimit { get; set; }
    public List<JudgeTestCaseInfo> TestCases { get; set; } = new();
    public string JudgeMode { get; set; } = "Standard";
    public string? SpecialJudgeCode { get; set; }
}

/// <summary>
/// Judger 调度服务实现
/// </summary>
public class JudgerDispatchService : IJudgerDispatchService
{
    private readonly ConcurrentDictionary<Guid, JudgerNodeCache> _judgers = new();
    private readonly ConcurrentQueue<JudgeTaskInternal> _taskQueue = new();
    private readonly ConcurrentDictionary<Guid, JudgeTaskInternal> _runningTasks = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JudgerDispatchService> _logger;
    private readonly Timer _healthCheckTimer;
    
    public JudgerDispatchService(IServiceProvider serviceProvider, ILogger<JudgerDispatchService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // 定期检查 Judger 健康状态和超时任务
        _healthCheckTimer = new Timer(HealthCheck, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }
    
    #region Judger 管理
    
    public async Task<JudgerNodeInfo> RegisterJudgerAsync(
        string name,
        string secret,
        int maxConcurrent,
        List<string> languages,
        CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var secretHash = BCrypt.Net.BCrypt.HashPassword(secret);

        // Persist to DB so it survives backend restarts.
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var node = new JudgerNode
            {
                Id = id,
                Name = name,
                SecretHash = secretHash,
                MaxConcurrentTasks = maxConcurrent,
                IsEnabled = true,
                SupportedLanguages = languages is { Count: > 0 } ? string.Join(',', languages) : null,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.JudgerNodes.Add(node);
            await dbContext.SaveChangesAsync(ct);
        }

        var judger = new JudgerNodeCache
        {
            Id = id,
            Name = name,
            SecretHash = secretHash,
            MaxConcurrentTasks = maxConcurrent,
            SupportedLanguages = languages,
            Status = "Offline"
        };

        _judgers[judger.Id] = judger;
        _logger.LogInformation("Judger 注册成功: {Name} ({Id})", name, judger.Id);

        return ToNodeInfo(judger);
    }
    
    public async Task<JudgerNodeInfo?> AuthenticateJudgerAsync(Guid judgerId, string secret, CancellationToken ct = default)
    {
        if (!_judgers.TryGetValue(judgerId, out var judger))
        {
            judger = await TryLoadJudgerFromDbAsync(judgerId, ct);
            if (judger == null)
            {
                return null;
            }
        }

        // Always honor DB enable/delete state even if already cached.
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var enabled = await dbContext.JudgerNodes
                .AsNoTracking()
                .Where(n => n.Id == judgerId && !n.IsDeleted)
                .Select(n => (bool?)n.IsEnabled)
                .FirstOrDefaultAsync(ct);

            if (enabled != true)
            {
                return null;
            }
        }

        if (!BCrypt.Net.BCrypt.Verify(secret, judger.SecretHash))
        {
            return null;
        }

        judger.Status = "Online";
        judger.LastHeartbeat = DateTime.UtcNow;

        // Best-effort: update last connected time in DB.
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var node = await dbContext.JudgerNodes.FirstOrDefaultAsync(n => n.Id == judgerId && !n.IsDeleted, CancellationToken.None);
                if (node != null)
                {
                    node.LastConnectedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync(CancellationToken.None);
                }
            }
            catch
            {
                // ignore
            }
        });

        _logger.LogInformation("Judger 认证成功: {Name} ({Id})", judger.Name, judger.Id);
        return ToNodeInfo(judger);
    }
    
    public Task UpdateHeartbeatAsync(Guid judgerId, CancellationToken ct = default)
    {
        if (_judgers.TryGetValue(judgerId, out var judger))
        {
            judger.LastHeartbeat = DateTime.UtcNow;
            if (judger.Status == "Offline")
            {
                judger.Status = "Online";
            }
        }
        return Task.CompletedTask;
    }
    
    public async Task<IReadOnlyList<JudgerNodeInfo>> GetAllJudgersAsync(CancellationToken ct = default)
    {
        await EnsureCacheLoadedAsync(ct);
        return _judgers.Values.Select(ToNodeInfo).ToList();
    }
    
    public async Task RemoveJudgerAsync(Guid judgerId, CancellationToken ct = default)
    {
        if (_judgers.TryRemove(judgerId, out var judger))
        {
            _logger.LogInformation("Judger 已移除(缓存): {Name} ({Id})", judger.Name, judger.Id);
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var node = await dbContext.JudgerNodes.FirstOrDefaultAsync(n => n.Id == judgerId && !n.IsDeleted, ct);
        if (node != null)
        {
            node.IsDeleted = true;
            node.DeletedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(ct);
        }
    }

    private async Task EnsureCacheLoadedAsync(CancellationToken ct)
    {
        if (!_judgers.IsEmpty)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var nodes = await dbContext.JudgerNodes
            .AsNoTracking()
            .Where(n => !n.IsDeleted)
            .ToListAsync(ct);

        foreach (var node in nodes)
        {
            _judgers.TryAdd(node.Id, new JudgerNodeCache
            {
                Id = node.Id,
                Name = node.Name,
                SecretHash = node.SecretHash,
                MaxConcurrentTasks = node.MaxConcurrentTasks,
                SupportedLanguages = ParseLanguages(node.SupportedLanguages),
                Status = "Offline",
                LastHeartbeat = null
            });
        }
    }

    private async Task<JudgerNodeCache?> TryLoadJudgerFromDbAsync(Guid judgerId, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var node = await dbContext.JudgerNodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == judgerId && !n.IsDeleted, ct);
        if (node == null || !node.IsEnabled)
        {
            return null;
        }

        var cache = new JudgerNodeCache
        {
            Id = node.Id,
            Name = node.Name,
            SecretHash = node.SecretHash,
            MaxConcurrentTasks = node.MaxConcurrentTasks,
            SupportedLanguages = ParseLanguages(node.SupportedLanguages),
            Status = "Offline"
        };

        _judgers.TryAdd(cache.Id, cache);
        return cache;
    }

    private static List<string> ParseLanguages(string? langs)
    {
        if (string.IsNullOrWhiteSpace(langs))
        {
            return new List<string>();
        }

        return langs
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
    
    private static JudgerNodeInfo ToNodeInfo(JudgerNodeCache node) => new()
    {
        Id = node.Id,
        Name = node.Name,
        MaxConcurrentTasks = node.MaxConcurrentTasks,
        CurrentTasks = node.CurrentTasks,
        Status = node.Status,
        LastHeartbeat = node.LastHeartbeat,
        SupportedLanguages = node.SupportedLanguages
    };
    
    #endregion
    
    #region 任务管理
    
    public async Task EnqueueTaskAsync(Submission submission, CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // 加载题目和测试用例
        var problem = await dbContext.Problems
            .Include(p => p.TestCases)
            .FirstOrDefaultAsync(p => p.Id == submission.ProblemId, ct);
        
        if (problem == null)
        {
            _logger.LogError("题目不存在: {ProblemId}", submission.ProblemId);
            return;
        }
        
        var task = new JudgeTaskInternal
        {
            Id = Guid.NewGuid(),
            SubmissionId = submission.Id,
            Code = submission.Code,
            Language = submission.Language,
            TimeLimit = problem.TimeLimit,
            MemoryLimit = problem.MemoryLimit,
            JudgeMode = problem.JudgeMode.ToString(),
            SpecialJudgeCode = problem.SpecialJudgeCode,
            TestCases = problem.TestCases?.Select(tc => new JudgeTestCaseInfo
            {
                Order = tc.Order,
                InputPath = tc.InputPath,
                OutputPath = tc.OutputPath,
                Score = tc.Score
            }).ToList() ?? new()
        };
        
        _taskQueue.Enqueue(task);
        _logger.LogInformation("任务已入队: {SubmissionId}", submission.Id);
    }
    
    public Task<JudgeTaskInfo?> FetchTaskAsync(Guid judgerId, CancellationToken ct = default)
    {
        if (!_judgers.TryGetValue(judgerId, out var judger))
        {
            return Task.FromResult<JudgeTaskInfo?>(null);
        }
        
        // 检查是否还能接受新任务
        if (judger.CurrentTasks >= judger.MaxConcurrentTasks)
        {
            return Task.FromResult<JudgeTaskInfo?>(null);
        }
        
        // 尝试获取任务
        if (_taskQueue.TryDequeue(out var task))
        {
            task.AssignedJudgerId = judgerId;
            task.AssignedAt = DateTime.UtcNow;
            
            _runningTasks[task.Id] = task;
            judger.CurrentTasks++;
            
            if (judger.CurrentTasks >= judger.MaxConcurrentTasks)
            {
                judger.Status = "Busy";
            }
            
            _logger.LogInformation("任务已分配: {SubmissionId} -> {JudgerName}", task.SubmissionId, judger.Name);
            
            return Task.FromResult<JudgeTaskInfo?>(new JudgeTaskInfo
            {
                TaskId = task.Id,
                SubmissionId = task.SubmissionId,
                Code = task.Code,
                Language = task.Language,
                TimeLimit = task.TimeLimit,
                MemoryLimit = task.MemoryLimit,
                JudgeMode = task.JudgeMode,
                SpecialJudgeCode = task.SpecialJudgeCode,
                TestCases = task.TestCases
            });
        }
        
        return Task.FromResult<JudgeTaskInfo?>(null);
    }
    
    public async Task ReportResultAsync(Guid judgerId, JudgeResultInfo result, CancellationToken ct = default)
    {
        if (!_judgers.TryGetValue(judgerId, out var judger))
        {
            _logger.LogWarning("未知的 Judger 上报结果: {JudgerId}", judgerId);
            return;
        }
        
        // 减少当前任务数
        judger.CurrentTasks = Math.Max(0, judger.CurrentTasks - 1);
        if (judger.Status == "Busy" && judger.CurrentTasks < judger.MaxConcurrentTasks)
        {
            judger.Status = "Online";
        }
        
        // 从运行中任务移除
        var taskToRemove = _runningTasks.Values.FirstOrDefault(t => t.SubmissionId == result.SubmissionId);
        if (taskToRemove != null)
        {
            _runningTasks.TryRemove(taskToRemove.Id, out _);
        }
        
        // 更新数据库
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var submission = await dbContext.Submissions.FindAsync(new object[] { result.SubmissionId }, ct);
        if (submission == null)
        {
            _logger.LogWarning("提交不存在: {SubmissionId}", result.SubmissionId);
            return;
        }
        
        submission.Status = result.Status;
        submission.Score = result.Score;
        submission.TimeUsed = result.TimeUsed;
        submission.MemoryUsed = result.MemoryUsed;
        submission.CompileMessage = result.CompileMessage;
        submission.JudgeMessage = result.JudgeMessage;
        submission.JudgedAt = DateTime.UtcNow;
        
        // 保存测试点结果
        foreach (var testResult in result.TestResults)
        {
            dbContext.JudgeResults.Add(new JudgeResult
            {
                Id = Guid.NewGuid(),
                SubmissionId = submission.Id,
                TestCaseOrder = testResult.Order,
                Status = testResult.Status,
                TimeUsed = testResult.TimeUsed,
                MemoryUsed = testResult.MemoryUsed,
                Score = testResult.Score
            });
        }
        
        // 更新题目统计
        if (result.Status == JudgeStatus.Accepted)
        {
            var problem = await dbContext.Problems.FindAsync(new object[] { submission.ProblemId }, ct);
            if (problem != null)
            {
                problem.AcceptedCount++;
            }
        }
        
        await dbContext.SaveChangesAsync(ct);
        
        _logger.LogInformation("评测结果已保存: {SubmissionId}, 状态: {Status}", result.SubmissionId, result.Status);
    }
    
    public Task<int> GetPendingTaskCountAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_taskQueue.Count);
    }
    
    #endregion
    
    #region 健康检查
    
    private void HealthCheck(object? state)
    {
        var now = DateTime.UtcNow;
        var timeout = TimeSpan.FromMinutes(1);
        
        // 检查 Judger 心跳
        foreach (var judger in _judgers.Values)
        {
            if (judger.Status != "Offline" && 
                judger.LastHeartbeat.HasValue && 
                now - judger.LastHeartbeat.Value > timeout)
            {
                judger.Status = "Offline";
                _logger.LogWarning("Judger 心跳超时: {Name} ({Id})", judger.Name, judger.Id);
                
                // 重新入队该 Judger 的任务
                var tasks = _runningTasks.Values
                    .Where(t => t.AssignedJudgerId == judger.Id)
                    .ToList();
                
                foreach (var task in tasks)
                {
                    if (_runningTasks.TryRemove(task.Id, out _))
                    {
                        task.AssignedJudgerId = null;
                        task.RetryCount++;
                        
                        if (task.RetryCount < 3)
                        {
                            _taskQueue.Enqueue(task);
                            _logger.LogInformation("任务重新入队: {SubmissionId}", task.SubmissionId);
                        }
                        else
                        {
                            _logger.LogError("任务重试次数过多，已放弃: {SubmissionId}", task.SubmissionId);
                        }
                    }
                }
                
                judger.CurrentTasks = 0;
            }
        }
    }
    
    #endregion
}

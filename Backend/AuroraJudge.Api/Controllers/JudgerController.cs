using AuroraJudge.Application.Services;
using AuroraJudge.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuroraJudge.Api.Controllers;

/// <summary>
/// Judger 评测节点 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class JudgerController : ControllerBase
{
    private readonly IJudgerDispatchService _dispatchService;
    private readonly ILogger<JudgerController> _logger;

    public JudgerController(IJudgerDispatchService dispatchService, ILogger<JudgerController> logger)
    {
        _dispatchService = dispatchService;
        _logger = logger;
    }

    /// <summary>
    /// Judger 连接并认证
    /// </summary>
    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] JudgerConnectRequest request, CancellationToken ct)
    {
        var judger = await _dispatchService.AuthenticateJudgerAsync(request.JudgerId, request.Secret, ct);
        if (judger == null)
        {
            return Unauthorized(new { Message = "认证失败：Judger ID 或 Secret 错误" });
        }

        return Ok(new JudgerConnectResponse
        {
            JudgerId = judger.Id,
            Name = judger.Name,
            MaxConcurrentTasks = judger.MaxConcurrentTasks,
            Message = "连接成功"
        });
    }

    /// <summary>
    /// Judger 心跳
    /// </summary>
    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] JudgerHeartbeatRequest request, CancellationToken ct)
    {
        var judger = await _dispatchService.AuthenticateJudgerAsync(request.JudgerId, request.Secret, ct);
        if (judger == null)
        {
            return Unauthorized();
        }

        await _dispatchService.UpdateHeartbeatAsync(request.JudgerId, ct);
        
        var pendingCount = await _dispatchService.GetPendingTaskCountAsync(ct);
        
        return Ok(new JudgerHeartbeatResponse
        {
            Status = "ok",
            PendingTasks = pendingCount,
            CurrentTasks = judger.CurrentTasks
        });
    }

    /// <summary>
    /// 获取评测任务
    /// </summary>
    [HttpPost("fetch")]
    public async Task<IActionResult> FetchTask([FromBody] JudgerFetchRequest request, CancellationToken ct)
    {
        var judger = await _dispatchService.AuthenticateJudgerAsync(request.JudgerId, request.Secret, ct);
        if (judger == null)
        {
            return Unauthorized();
        }

        var task = await _dispatchService.FetchTaskAsync(request.JudgerId, ct);
        if (task == null)
        {
            return Ok(new JudgerFetchResponse { HasTask = false });
        }

        return Ok(new JudgerFetchResponse
        {
            HasTask = true,
            Task = new JudgeTaskDto
            {
                TaskId = task.TaskId,
                SubmissionId = task.SubmissionId,
                Code = task.Code,
                Language = task.Language,
                TimeLimit = task.TimeLimit,
                MemoryLimit = task.MemoryLimit,
                JudgeMode = task.JudgeMode,
                SpecialJudgeCode = task.SpecialJudgeCode,
                TestCases = task.TestCases.Select(tc => new JudgeTestCaseDto
                {
                    Order = tc.Order,
                    InputPath = tc.InputPath,
                    OutputPath = tc.OutputPath,
                    Score = tc.Score
                }).ToList()
            }
        });
    }

    /// <summary>
    /// 上报评测结果
    /// </summary>
    [HttpPost("report")]
    public async Task<IActionResult> ReportResult([FromBody] JudgerReportRequest request, CancellationToken ct)
    {
        var judger = await _dispatchService.AuthenticateJudgerAsync(request.JudgerId, request.Secret, ct);
        if (judger == null)
        {
            return Unauthorized();
        }

        var result = new JudgeResultInfo
        {
            SubmissionId = request.SubmissionId,
            Status = Enum.Parse<Domain.Enums.JudgeStatus>(request.Status),
            Score = request.Score,
            TimeUsed = request.TimeUsed,
            MemoryUsed = request.MemoryUsed,
            CompileMessage = request.CompileMessage,
            JudgeMessage = request.JudgeMessage,
            TestResults = request.TestResults?.Select(tr => new TestCaseResultInfo
            {
                Order = tr.Order,
                Status = Enum.Parse<Domain.Enums.JudgeStatus>(tr.Status),
                TimeUsed = tr.TimeUsed,
                MemoryUsed = tr.MemoryUsed,
                Score = tr.Score,
                Message = tr.Message
            }).ToList() ?? new()
        };

        await _dispatchService.ReportResultAsync(request.JudgerId, result, ct);

        return Ok(new { Message = "结果已接收" });
    }

    /// <summary>
    /// 获取所有 Judger 状态（管理员）
    /// </summary>
    [HttpGet("status")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var judgers = await _dispatchService.GetAllJudgersAsync(ct);
        var pendingCount = await _dispatchService.GetPendingTaskCountAsync(ct);

        return Ok(new JudgerStatusResponse
        {
            Judgers = judgers.Select(j => new JudgerInfoDto
            {
                Id = j.Id,
                Name = j.Name,
                Status = j.Status,
                CurrentTasks = j.CurrentTasks,
                MaxConcurrentTasks = j.MaxConcurrentTasks,
                LastHeartbeat = j.LastHeartbeat,
                SupportedLanguages = j.SupportedLanguages
            }).ToList(),
            PendingTasks = pendingCount
        });
    }

    /// <summary>
    /// 注册新 Judger（管理员）
    /// </summary>
    [HttpPost("register")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> RegisterJudger([FromBody] JudgerRegisterRequest request, CancellationToken ct)
    {
        // 生成随机密钥
        var secret = Guid.NewGuid().ToString("N");
        
        var judger = await _dispatchService.RegisterJudgerAsync(
            request.Name,
            secret,
            request.MaxConcurrentTasks,
            request.SupportedLanguages,
            ct
        );

        return Ok(new JudgerRegisterResponse
        {
            JudgerId = judger.Id,
            Name = judger.Name,
            Secret = secret, // 只在注册时返回一次明文密钥
            Message = "Judger 注册成功，请保存好 Secret，此密钥只显示一次"
        });
    }

    /// <summary>
    /// 移除 Judger（管理员）
    /// </summary>
    [HttpDelete("{judgerId}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> RemoveJudger(Guid judgerId, CancellationToken ct)
    {
        await _dispatchService.RemoveJudgerAsync(judgerId, ct);
        return Ok(new { Message = "Judger 已移除" });
    }
}

#region Request/Response DTOs

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

public class JudgerHeartbeatResponse
{
    public string Status { get; set; } = string.Empty;
    public int PendingTasks { get; set; }
    public int CurrentTasks { get; set; }
}

public class JudgerFetchRequest
{
    public Guid JudgerId { get; set; }
    public string Secret { get; set; } = string.Empty;
}

public class JudgerFetchResponse
{
    public bool HasTask { get; set; }
    public JudgeTaskDto? Task { get; set; }
}

public class JudgeTaskDto
{
    public Guid TaskId { get; set; }
    public Guid SubmissionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int TimeLimit { get; set; }
    public int MemoryLimit { get; set; }
    public string JudgeMode { get; set; } = string.Empty;
    public string? SpecialJudgeCode { get; set; }
    public List<JudgeTestCaseDto> TestCases { get; set; } = new();
}

public class JudgeTestCaseDto
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
    public List<TestCaseResultDto>? TestResults { get; set; }
}

public class TestCaseResultDto
{
    public int Order { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TimeUsed { get; set; }
    public int MemoryUsed { get; set; }
    public int Score { get; set; }
    public string? Message { get; set; }
}

public class JudgerStatusResponse
{
    public List<JudgerInfoDto> Judgers { get; set; } = new();
    public int PendingTasks { get; set; }
}

public class JudgerInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int CurrentTasks { get; set; }
    public int MaxConcurrentTasks { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public List<string> SupportedLanguages { get; set; } = new();
}

public class JudgerRegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public int MaxConcurrentTasks { get; set; } = 4;
    public List<string> SupportedLanguages { get; set; } = new() { "c", "cpp", "java", "python", "go", "rust" };
}

public class JudgerRegisterResponse
{
    public Guid JudgerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

#endregion

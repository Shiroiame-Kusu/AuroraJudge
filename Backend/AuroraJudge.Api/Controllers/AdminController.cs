using AuroraJudge.Application.DTOs;
using AuroraJudge.Application.Services;
using AuroraJudge.Shared.Constants;
using AuroraJudge.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AuroraJudge.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IJudgerDispatchService _judgerDispatchService;
    private readonly ILogger<AdminController> _logger;
    
    public AdminController(IAdminService adminService, IJudgerDispatchService judgerDispatchService, ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _judgerDispatchService = judgerDispatchService;
        _logger = logger;
    }
    
    #region 用户管理
    
    /// <summary>
    /// 获取用户列表
    /// </summary>
    [HttpGet("users")]
    [RequirePermission(Permissions.UserView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<UserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResponse<UserDto>>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _adminService.GetUsersAsync(page, pageSize, search, cancellationToken);
        return Ok(ApiResponse<PagedResponse<UserDto>>.Ok(result));
    }
    
    /// <summary>
    /// 禁用用户
    /// </summary>
    [HttpPost("users/{id:guid}/ban")]
    [RequirePermission(Permissions.UserBan)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> BanUser(Guid id, CancellationToken cancellationToken)
    {
        await _adminService.BanUserAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("用户已禁用"));
    }
    
    /// <summary>
    /// 解禁用户
    /// </summary>
    [HttpPost("users/{id:guid}/unban")]
    [RequirePermission(Permissions.UserBan)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> UnbanUser(Guid id, CancellationToken cancellationToken)
    {
        await _adminService.UnbanUserAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("用户已解禁"));
    }
    
    #endregion
    
    #region 角色权限管理
    
    /// <summary>
    /// 获取所有角色
    /// </summary>
    [HttpGet("roles")]
    [RequirePermission(Permissions.RoleView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RoleDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleDto>>>> GetRoles(CancellationToken cancellationToken)
    {
        var result = await _adminService.GetRolesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RoleDto>>.Ok(result));
    }
    
    /// <summary>
    /// 创建角色
    /// </summary>
    [HttpPost("roles")]
    [RequirePermission(Permissions.RoleCreate)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.CreateRoleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetRoles), null, ApiResponse<RoleDto>.Ok(result, "角色创建成功"));
    }
    
    /// <summary>
    /// 更新角色
    /// </summary>
    [HttpPut("roles/{id:guid}")]
    [RequirePermission(Permissions.RoleEdit)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateRoleAsync(id, request, cancellationToken);
        return Ok(ApiResponse<RoleDto>.Ok(result, "角色更新成功"));
    }
    
    /// <summary>
    /// 删除角色
    /// </summary>
    [HttpDelete("roles/{id:guid}")]
    [RequirePermission(Permissions.RoleDelete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        await _adminService.DeleteRoleAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("角色删除成功"));
    }
    
    /// <summary>
    /// 获取所有权限
    /// </summary>
    [HttpGet("permissions")]
    [RequirePermission(Permissions.PermissionView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PermissionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionDto>>>> GetPermissions(CancellationToken cancellationToken)
    {
        var result = await _adminService.GetPermissionsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PermissionDto>>.Ok(result));
    }
    
    /// <summary>
    /// 为用户分配角色
    /// </summary>
    [HttpPost("users/roles")]
    [RequirePermission(Permissions.PermissionAssign)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> AssignRole([FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        var operatorId = User.GetUserId();
        await _adminService.AssignRoleAsync(request.UserId, request.RoleId, operatorId, cancellationToken);
        return Ok(ApiResponse.Ok("角色分配成功"));
    }
    
    /// <summary>
    /// 移除用户角色
    /// </summary>
    [HttpDelete("users/{userId:guid}/roles/{roleId:guid}")]
    [RequirePermission(Permissions.PermissionAssign)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> RemoveRole(Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        await _adminService.RemoveRoleAsync(userId, roleId, cancellationToken);
        return Ok(ApiResponse.Ok("角色移除成功"));
    }
    
    #endregion
    
    #region 系统配置
    
    /// <summary>
    /// 获取系统配置
    /// </summary>
    [HttpGet("settings")]
    [RequirePermission(Permissions.SystemSettings)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SystemConfigDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SystemConfigDto>>>> GetSettings(CancellationToken cancellationToken)
    {
        var result = await _adminService.GetSystemConfigsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SystemConfigDto>>.Ok(result));
    }
    
    /// <summary>
    /// 更新系统配置
    /// </summary>
    [HttpPut("settings/{key}")]
    [RequirePermission(Permissions.SystemSettings)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> UpdateSetting(string key, [FromBody] UpdateSystemConfigRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        await _adminService.UpdateSystemConfigAsync(key, request.Value, userId, cancellationToken);
        return Ok(ApiResponse.Ok("配置更新成功"));
    }
    
    #endregion
    
    #region 语言配置
    
    /// <summary>
    /// 获取语言配置
    /// </summary>
    [HttpGet("languages")]
    [RequirePermission(Permissions.SystemLanguages)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LanguageConfigDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LanguageConfigDto>>>> GetLanguages(CancellationToken cancellationToken)
    {
        var result = await _adminService.GetLanguageConfigsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<LanguageConfigDto>>.Ok(result));
    }
    
    /// <summary>
    /// 创建语言配置
    /// </summary>
    [HttpPost("languages")]
    [RequirePermission(Permissions.SystemLanguages)]
    [ProducesResponseType(typeof(ApiResponse<LanguageConfigDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<LanguageConfigDto>>> CreateLanguage([FromBody] CreateLanguageConfigRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.CreateLanguageConfigAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetLanguages), null, ApiResponse<LanguageConfigDto>.Ok(result, "语言配置创建成功"));
    }
    
    #endregion
    
    #region 判题机管理
    
    /// <summary>
    /// 获取判题机状态
    /// </summary>
    [HttpGet("judgers")]
    [RequirePermission(Permissions.SystemJudger)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<JudgerStatusDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<JudgerStatusDto>>>> GetJudgers(CancellationToken cancellationToken)
    {
        var result = await _adminService.GetJudgerStatusesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<JudgerStatusDto>>.Ok(result));
    }
    
    /// <summary>
    /// 启用/禁用判题机
    /// </summary>
    [HttpPut("judgers/{id:guid}/enabled")]
    [RequirePermission(Permissions.SystemJudger)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> SetJudgerEnabled(Guid id, [FromQuery] bool enabled, CancellationToken cancellationToken)
    {
        await _adminService.SetJudgerEnabledAsync(id, enabled, cancellationToken);
        return Ok(ApiResponse.Ok(enabled ? "判题机已启用" : "判题机已禁用"));
    }

    /// <summary>
    /// 生成 Judger 配置（注册新 Judger 并返回 judger.conf 内容）
    /// </summary>
    [HttpPost("judgers/config")]
    [RequirePermission(Permissions.SystemJudger)]
    [ProducesResponseType(typeof(ApiResponse<GenerateJudgerConfigResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GenerateJudgerConfigResponse>>> GenerateJudgerConfig(
        [FromBody] GenerateJudgerConfigRequest request,
        CancellationToken cancellationToken)
    {
        var secret = Guid.NewGuid().ToString("N");
        var supportedLanguages = request.SupportedLanguages ?? new();
        var maxConcurrentTasks = request.MaxConcurrentTasks <= 0 ? 4 : request.MaxConcurrentTasks;

        var judger = await _judgerDispatchService.RegisterJudgerAsync(
            request.Name,
            secret,
            maxConcurrentTasks,
            supportedLanguages,
            cancellationToken);

        var mode = string.Equals(request.Mode, "rabbitmq", StringComparison.OrdinalIgnoreCase) ? "rabbitmq" : "http";
        var backendUrl = string.IsNullOrWhiteSpace(request.BackendUrl)
            ? $"{Request.Scheme}://{Request.Host}" 
            : request.BackendUrl.Trim().TrimEnd('/');

        var configText = JudgerConfigTextBuilder.BuildJudgerConf(
            mode,
            request.Name,
            request.WorkDir,
            maxConcurrentTasks,
            backendUrl,
            judger.Id,
            secret,
            request.PollIntervalMs,
            request.RabbitMqConnection,
            request.LogLevel);

        return Ok(ApiResponse<GenerateJudgerConfigResponse>.Ok(new GenerateJudgerConfigResponse
        {
            JudgerId = judger.Id,
            Name = request.Name,
            Secret = secret,
            Mode = mode,
            BackendUrl = backendUrl,
            ConfigText = configText
        }));
    }

    /// <summary>
    /// 获取 Judger 运行时状态（用于环境检测/实时状态查看）
    /// </summary>
    [HttpGet("judgers/runtime-status")]
    [RequirePermission(Permissions.SystemJudger)]
    [ProducesResponseType(typeof(ApiResponse<JudgerRuntimeStatusResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<JudgerRuntimeStatusResponse>>> GetJudgerRuntimeStatus(CancellationToken cancellationToken)
    {
        var judgers = await _judgerDispatchService.GetAllJudgersAsync(cancellationToken);
        var pendingCount = await _judgerDispatchService.GetPendingTaskCountAsync(cancellationToken);

        return Ok(ApiResponse<JudgerRuntimeStatusResponse>.Ok(new JudgerRuntimeStatusResponse
        {
            PendingTasks = pendingCount,
            Judgers = judgers
        }));
    }
    
    #endregion
    
    #region 审计日志
    
    /// <summary>
    /// 获取审计日志
    /// </summary>
    [HttpGet("audit-logs")]
    [RequirePermission(Permissions.SystemAuditLog)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<AuditLogDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResponse<AuditLogDto>>>> GetAuditLogs(
        [FromQuery] AuditLogQueryRequest query,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.GetAuditLogsAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResponse<AuditLogDto>>.Ok(result));
    }
    
    #endregion
}

public class GenerateJudgerConfigRequest
{
    public string Name { get; set; } = "judger-1";
    public int MaxConcurrentTasks { get; set; } = 4;
    public List<string>? SupportedLanguages { get; set; }

    /// <summary>http（默认）或 rabbitmq</summary>
    public string? Mode { get; set; }

    /// <summary>HTTP 模式下 Judger 访问后端的 BaseUrl（例如 http://localhost:5000）</summary>
    public string? BackendUrl { get; set; }

    public int? PollIntervalMs { get; set; }
    public string? WorkDir { get; set; }

    /// <summary>RabbitMQ 模式下连接串（可选）</summary>
    public string? RabbitMqConnection { get; set; }

    public string? LogLevel { get; set; }
}

public class GenerateJudgerConfigResponse
{
    public Guid JudgerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string Mode { get; set; } = "http";
    public string BackendUrl { get; set; } = string.Empty;
    public string ConfigText { get; set; } = string.Empty;
}

public class JudgerRuntimeStatusResponse
{
    public int PendingTasks { get; set; }
    public IReadOnlyList<JudgerNodeInfo> Judgers { get; set; } = Array.Empty<JudgerNodeInfo>();
}


internal static class JudgerConfigTextBuilder
{
    internal static string BuildJudgerConf(
        string mode,
        string name,
        string? workDir,
        int maxConcurrentTasks,
        string backendUrl,
        Guid judgerId,
        string secret,
        int? pollIntervalMs,
        string? rabbitMqConnection,
        string? logLevel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# AuroraJudge Judger 配置文件");
        sb.AppendLine("# 保存为 judger.conf");
        sb.AppendLine();

        sb.AppendLine("[judger]");
        sb.AppendLine($"mode = {mode}");
        sb.AppendLine($"name = {name}");
        sb.AppendLine($"work_dir = {(!string.IsNullOrWhiteSpace(workDir) ? workDir : "/tmp/aurora-judge")}");
        sb.AppendLine($"max_concurrent_tasks = {maxConcurrentTasks}");
        sb.AppendLine();

        sb.AppendLine("[logging]");
        sb.AppendLine($"level = {(!string.IsNullOrWhiteSpace(logLevel) ? logLevel : "Information")}");
        sb.AppendLine();

        if (string.Equals(mode, "rabbitmq", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("[rabbitmq]");
            sb.AppendLine($"connection = {(!string.IsNullOrWhiteSpace(rabbitMqConnection) ? rabbitMqConnection : "amqp://guest:guest@localhost:5672")}");
            sb.AppendLine();
            return sb.ToString();
        }

        sb.AppendLine("[http]");
        sb.AppendLine($"backend_url = {backendUrl}");
        sb.AppendLine($"judger_id = {judgerId}");
        sb.AppendLine($"secret = {secret}");
        sb.AppendLine($"poll_interval_ms = {(pollIntervalMs.HasValue && pollIntervalMs.Value > 0 ? pollIntervalMs.Value : 1000)}");
        sb.AppendLine();

        return sb.ToString();
    }
}

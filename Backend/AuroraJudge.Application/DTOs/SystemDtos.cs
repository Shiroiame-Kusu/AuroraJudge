using AuroraJudge.Domain.Enums;

namespace AuroraJudge.Application.DTOs;

// 公告
public record AnnouncementDto(
    Guid Id,
    string Title,
    string Content,
    AnnouncementStatus Status,
    bool IsPinned,
    int ViewCount,
    Guid AuthorId,
    string AuthorName,
    DateTime? PublishedAt,
    DateTime CreatedAt
);

public record CreateAnnouncementRequest(
    string Title,
    string Content,
    AnnouncementStatus Status,
    bool IsPinned
);

// 角色和权限
public record RoleDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    bool IsSystem,
    int Priority,
    IReadOnlyList<string> Permissions
);

public record CreateRoleRequest(
    string Name,
    string Code,
    string? Description,
    int Priority,
    IReadOnlyList<string> PermissionCodes
);

public record UpdateRoleRequest(
    string Name,
    string? Description,
    int Priority,
    IReadOnlyList<string> PermissionCodes
);

public record PermissionDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Category
);

public record AssignRoleRequest(
    Guid UserId,
    Guid RoleId
);

public record GrantPermissionRequest(
    Guid UserId,
    Guid PermissionId,
    DateTime? ExpiresAt
);

// 系统配置
public record SystemConfigDto(
    Guid Id,
    string Key,
    string Value,
    string Type,
    string Category,
    string? Description,
    bool IsPublic,
    DateTime UpdatedAt
);

public record UpdateSystemConfigRequest(
    string Value
);

// 语言配置
public record LanguageConfigDto(
    Guid Id,
    string Code,
    string Name,
    string? Version,
    bool IsEnabled,
    string? CompileCommand,
    string RunCommand,
    string SourceFileName,
    string? ExecutableFileName,
    int CompileTimeLimit,
    int CompileMemoryLimit,
    double TimeMultiplier,
    double MemoryMultiplier,
    string? MonacoLanguage,
    string? Template,
    int Order
);

public record CreateLanguageConfigRequest(
    string Code,
    string Name,
    string? Version,
    bool IsEnabled,
    string? CompileCommand,
    string RunCommand,
    string SourceFileName,
    string? ExecutableFileName,
    int CompileTimeLimit,
    int CompileMemoryLimit,
    double TimeMultiplier,
    double MemoryMultiplier,
    string? MonacoLanguage,
    string? Template,
    int Order
);

// 判题机状态
public record JudgerStatusDto(
    Guid Id,
    string JudgerId,
    string Name,
    string? Hostname,
    bool IsOnline,
    bool IsEnabled,
    int CurrentTasks,
    int MaxTasks,
    double? CpuUsage,
    double? MemoryUsage,
    long CompletedTasks,
    string? Version,
    DateTime? LastHeartbeat,
    DateTime? StartedAt
);

// 审计日志
public record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string? Username,
    AuditAction Action,
    string? EntityType,
    string? EntityId,
    string Description,
    string? IpAddress,
    DateTime Timestamp
);

public record AuditLogQueryRequest(
    int Page = 1,
    int PageSize = 20,
    Guid? UserId = null,
    AuditAction? Action = null,
    string? EntityType = null,
    DateTime? StartTime = null,
    DateTime? EndTime = null
);

// 功能开关
public record FeatureFlagDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsEnabled,
    DateTime UpdatedAt
);

public record UpdateFeatureFlagRequest(
    bool IsEnabled,
    string? Conditions
);

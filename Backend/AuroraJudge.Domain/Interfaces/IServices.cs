using AuroraJudge.Domain.Common;
using AuroraJudge.Domain.Entities;
using AuroraJudge.Domain.Enums;

namespace AuroraJudge.Domain.Interfaces;

/// <summary>
/// 权限服务接口
/// </summary>
public interface IPermissionService
{
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default);
    Task<bool> HasAnyPermissionAsync(Guid userId, IEnumerable<string> permissionCodes, CancellationToken cancellationToken = default);
    Task<bool> HasAllPermissionsAsync(Guid userId, IEnumerable<string> permissionCodes, CancellationToken cancellationToken = default);
    Task<bool> HasRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task InvalidateUserPermissionsCacheAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 缓存服务接口
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// 消息队列服务接口
/// </summary>
public interface IMessageQueueService
{
    /// <summary>
    /// 消息队列是否可用
    /// </summary>
    bool IsEnabled { get; }
    
    Task PublishAsync<T>(string queue, T message, CancellationToken cancellationToken = default);
    Task SubscribeAsync<T>(string queue, Func<T, Task> handler, CancellationToken cancellationToken = default);
}

/// <summary>
/// 文件存储服务接口
/// </summary>
public interface IStorageService
{
    Task UploadAsync(string path, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default);
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);
    Task<string> GetUrlAsync(string path, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// 审计日志服务接口
/// </summary>
public interface IAuditLogService
{
    Task LogAsync(
        Guid? userId,
        string? username,
        AuditAction action,
        string description,
        string? entityType = null,
        string? entityId = null,
        object? oldValue = null,
        object? newValue = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);
    
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        Guid? userId = null, 
        string? action = null, 
        DateTime? startTime = null, 
        DateTime? endTime = null, 
        CancellationToken cancellationToken = default);
}

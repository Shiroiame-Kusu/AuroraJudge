using AuroraJudge.Domain.Interfaces;
using AuroraJudge.Infrastructure.Persistence;
using AuroraJudge.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace AuroraJudge.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    
    public PermissionService(ApplicationDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }
    
    public async Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default)
    {
        var permissions = await GetUserPermissionsAsync(userId, cancellationToken);
        return permissions.Contains(permissionCode) || permissions.Contains(Permissions.All);
    }
    
    public async Task<bool> HasAnyPermissionAsync(Guid userId, IEnumerable<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        var permissions = await GetUserPermissionsAsync(userId, cancellationToken);
        if (permissions.Contains(Permissions.All))
        {
            return true;
        }
        
        return permissionCodes.Any(p => permissions.Contains(p));
    }
    
    public async Task<bool> HasAllPermissionsAsync(Guid userId, IEnumerable<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        var permissions = await GetUserPermissionsAsync(userId, cancellationToken);
        if (permissions.Contains(Permissions.All))
        {
            return true;
        }
        
        return permissionCodes.All(p => permissions.Contains(p));
    }
    
    public async Task<bool> HasRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        var roles = await GetUserRolesAsync(userId, cancellationToken);
        return roles.Contains(roleName);
    }
    
    public async Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.UserPermissions(userId);
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        
        if (!string.IsNullOrEmpty(cached))
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(cached) ?? [];
        }
        
        // 获取角色权限
        var rolePermissions = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .ToListAsync(cancellationToken);
        
        // 获取直接分配的权限
        var directPermissions = await _context.UserPermissions
            .Where(up => up.UserId == userId)
            .Select(up => up.Permission.Code)
            .ToListAsync(cancellationToken);
        
        var allPermissions = rolePermissions.Union(directPermissions).Distinct().ToList();
        
        // 缓存 5 分钟
        await _cache.SetStringAsync(
            cacheKey,
            System.Text.Json.JsonSerializer.Serialize(allPermissions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            cancellationToken);
        
        return allPermissions;
    }
    
    public async Task<IReadOnlyList<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Code)
            .ToListAsync(cancellationToken);
    }
    
    public async Task InvalidateUserPermissionsCacheAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(CacheKeys.UserPermissions(userId), cancellationToken);
    }
}

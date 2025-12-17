using AuroraJudge.Domain.Common;
using AuroraJudge.Domain.Entities;
using AuroraJudge.Domain.Enums;
using AuroraJudge.Domain.Interfaces;
using AuroraJudge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuroraJudge.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    
    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
    
    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }
    
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }
    
    public async Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default)
    {
        usernameOrEmail = usernameOrEmail.Trim();
        var normalized = usernameOrEmail.ToLower();

        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(
                u => u.Username.ToLower() == normalized || u.Email.ToLower() == normalized,
                cancellationToken);
    }
    
    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, cancellationToken);
    }
    
    public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(u => u.Username == username, cancellationToken);
    }
    
    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(u => u.Email == email, cancellationToken);
    }
    
    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsQueryable();
        
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => 
                u.Username.Contains(search) || 
                u.Email.Contains(search) ||
                (u.DisplayName != null && u.DisplayName.Contains(search)));
        }
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        
        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetLeaderboardPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.Status == UserStatus.Active);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(u => u.Rating)
            .ThenByDescending(u => u.SolvedCount)
            .ThenBy(u => u.SubmissionCount)
            .ThenBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
    
    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }
    
    public async Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var exists = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
        
        if (!exists)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow
            });
        }
    }
    
    public async Task RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
        
        if (userRole != null)
        {
            _context.UserRoles.Remove(userRole);
        }
    }
    
    // 角色管理
    public async Task<IReadOnlyList<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<Role?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }
    
    public async Task AddRoleAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _context.Roles.AddAsync(role, cancellationToken);
    }
    
    public async Task DeleteRoleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await _context.Roles.FindAsync([id], cancellationToken);
        if (role != null)
        {
            _context.Roles.Remove(role);
        }
    }
    
    public async Task AssignPermissionToRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var exists = await _context.RolePermissions
            .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);
        
        if (!exists)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });
        }
    }
    
    public async Task ClearRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var permissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);
        
        _context.RolePermissions.RemoveRange(permissions);
    }
    
    // 权限管理
    public async Task<IReadOnlyList<Permission>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Permissions.ToListAsync(cancellationToken);
    }
    
    public async Task<Permission?> GetPermissionByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions.FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
    }
    
    // 系统配置
    public async Task<IReadOnlyList<SystemConfig>> GetSystemConfigsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SystemConfigs.ToListAsync(cancellationToken);
    }
    
    public async Task<SystemConfig?> GetSystemConfigByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _context.SystemConfigs.FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
    }
    
    // 语言配置
    public async Task<IReadOnlyList<LanguageConfig>> GetLanguageConfigsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LanguageConfigs.ToListAsync(cancellationToken);
    }
    
    public async Task AddLanguageConfigAsync(LanguageConfig config, CancellationToken cancellationToken = default)
    {
        await _context.LanguageConfigs.AddAsync(config, cancellationToken);
    }
    
    // 判题机
    public async Task<IReadOnlyList<JudgerStatus>> GetJudgerStatusesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.JudgerStatuses.ToListAsync(cancellationToken);
    }
    
    public async Task<JudgerStatus?> GetJudgerStatusByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.JudgerStatuses.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<JudgerNode>> GetJudgerNodesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.JudgerNodes
            .Where(n => !n.IsDeleted)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<JudgerNode?> GetJudgerNodeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.JudgerNodes.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted, cancellationToken);
    }
}

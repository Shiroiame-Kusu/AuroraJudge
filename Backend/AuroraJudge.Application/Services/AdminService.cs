using AuroraJudge.Application.DTOs;
using AuroraJudge.Domain.Common;
using AuroraJudge.Domain.Entities;
using AuroraJudge.Domain.Enums;
using AuroraJudge.Domain.Interfaces;
using AuroraJudge.Shared.Models;

namespace AuroraJudge.Application.Services;

public class AdminService : IAdminService
{
    private readonly IUserRepository _userRepository;
    private readonly IJudgerDispatchService _judgerDispatchService;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;
    
    public AdminService(
        IUserRepository userRepository,
        IJudgerDispatchService judgerDispatchService,
        IAuditLogService auditLogService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _judgerDispatchService = judgerDispatchService;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
    }
    
    #region 用户管理
    
    public async Task<PagedResponse<UserDto>> GetUsersAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _userRepository.GetPagedAsync(page, pageSize, search, cancellationToken);
        
        var dtos = items.Select(MapToUserDto).ToList();
        
        return new PagedResponse<UserDto>
        {
            Items = dtos,
            Total = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
    
    public async Task BanUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("用户不存在");
        
        user.Status = UserStatus.Banned;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    
    public async Task UnbanUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("用户不存在");
        
        user.Status = UserStatus.Active;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    
    #endregion
    
    #region 角色权限管理
    
    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _userRepository.GetAllRolesAsync(cancellationToken);
        return roles.Select(MapToRoleDto).ToList();
    }
    
    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            Priority = request.Priority,
            CreatedAt = DateTime.UtcNow
        };
        
        await _userRepository.AddRoleAsync(role, cancellationToken);
        
        // Assign permissions
        if (request.PermissionCodes?.Any() == true)
        {
            foreach (var code in request.PermissionCodes)
            {
                var permission = await _userRepository.GetPermissionByCodeAsync(code, cancellationToken);
                if (permission != null)
                {
                    await _userRepository.AssignPermissionToRoleAsync(role.Id, permission.Id, cancellationToken);
                }
            }
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return MapToRoleDto(role);
    }
    
    public async Task<RoleDto> UpdateRoleAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await _userRepository.GetRoleByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("角色不存在");
        
        role.Name = request.Name;
        role.Description = request.Description;
        role.Priority = request.Priority;
        role.UpdatedAt = DateTime.UtcNow;
        
        // Update permissions
        await _userRepository.ClearRolePermissionsAsync(role.Id, cancellationToken);
        
        if (request.PermissionCodes?.Any() == true)
        {
            foreach (var code in request.PermissionCodes)
            {
                var permission = await _userRepository.GetPermissionByCodeAsync(code, cancellationToken);
                if (permission != null)
                {
                    await _userRepository.AssignPermissionToRoleAsync(role.Id, permission.Id, cancellationToken);
                }
            }
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return MapToRoleDto(role);
    }
    
    public async Task DeleteRoleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _userRepository.DeleteRoleAsync(id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _userRepository.GetAllPermissionsAsync(cancellationToken);
        return permissions.Select(MapToPermissionDto).ToList();
    }
    
    public async Task AssignRoleAsync(Guid userId, Guid roleId, Guid operatorId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("用户不存在");
        
        var role = await _userRepository.GetRoleByIdAsync(roleId, cancellationToken)
            ?? throw new KeyNotFoundException("角色不存在");
        
        await _userRepository.AssignRoleAsync(userId, roleId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Log audit
        await _auditLogService.LogAsync(
            userId: operatorId,
            username: null,
            action: AuditAction.PermissionChange,
            description: $"为用户 {user.Username} 分配角色 {role.Name}",
            entityType: "UserRole",
            entityId: userId.ToString(),
            newValue: new { Action = "Assign", RoleId = roleId, RoleName = role.Name },
            cancellationToken: cancellationToken);
    }
    
    public async Task RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        await _userRepository.RemoveRoleAsync(userId, roleId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    
    #endregion
    
    #region 系统配置
    
    public async Task<IReadOnlyList<SystemConfigDto>> GetSystemConfigsAsync(CancellationToken cancellationToken = default)
    {
        var configs = await _userRepository.GetSystemConfigsAsync(cancellationToken);
        return configs.Select(MapToSystemConfigDto).ToList();
    }
    
    public async Task UpdateSystemConfigAsync(string key, string value, Guid operatorId, CancellationToken cancellationToken = default)
    {
        var config = await _userRepository.GetSystemConfigByKeyAsync(key, cancellationToken)
            ?? throw new KeyNotFoundException("配置项不存在");
        
        var oldValue = config.Value;
        config.Value = value;
        config.UpdatedAt = DateTime.UtcNow;
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Log audit
        await _auditLogService.LogAsync(
            userId: operatorId,
            username: null,
            action: AuditAction.Update,
            description: $"更新系统配置 {key}",
            entityType: "SystemConfig",
            entityId: config.Id.ToString(),
            oldValue: new { Key = key, Value = oldValue },
            newValue: new { Key = key, Value = value },
            cancellationToken: cancellationToken);
    }
    
    #endregion
    
    #region 语言配置
    
    public async Task<IReadOnlyList<LanguageConfigDto>> GetLanguageConfigsAsync(CancellationToken cancellationToken = default)
    {
        var configs = await _userRepository.GetLanguageConfigsAsync(cancellationToken);
        return configs.Select(MapToLanguageConfigDto).ToList();
    }
    
    public async Task<LanguageConfigDto> CreateLanguageConfigAsync(CreateLanguageConfigRequest request, CancellationToken cancellationToken = default)
    {
        var config = new LanguageConfig
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Name = request.Name,
            Version = request.Version,
            IsEnabled = request.IsEnabled,
            CompileCommand = request.CompileCommand,
            RunCommand = request.RunCommand,
            SourceFileName = request.SourceFileName,
            ExecutableFileName = request.ExecutableFileName,
            CompileTimeLimit = request.CompileTimeLimit,
            CompileMemoryLimit = request.CompileMemoryLimit,
            TimeMultiplier = request.TimeMultiplier,
            MemoryMultiplier = request.MemoryMultiplier,
            MonacoLanguage = request.MonacoLanguage,
            Template = request.Template,
            Order = request.Order
        };
        
        await _userRepository.AddLanguageConfigAsync(config, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return MapToLanguageConfigDto(config);
    }
    
    #endregion
    
    #region 判题机管理
    
    public async Task<IReadOnlyList<JudgerStatusDto>> GetJudgerStatusesAsync(CancellationToken cancellationToken = default)
    {
        var nodes = await _userRepository.GetJudgerNodesAsync(cancellationToken);
        var runtime = await _judgerDispatchService.GetAllJudgersAsync(cancellationToken);
        var runtimeById = runtime.ToDictionary(j => j.Id, j => j);

        return nodes
            .Select(n => MapToJudgerStatusDto(n, runtimeById.GetValueOrDefault(n.Id)))
            .ToList();
    }
    
    public async Task SetJudgerEnabledAsync(Guid judgerId, bool enabled, CancellationToken cancellationToken = default)
    {
        var judger = await _userRepository.GetJudgerNodeByIdAsync(judgerId, cancellationToken)
            ?? throw new KeyNotFoundException("判题机不存在");

        judger.IsEnabled = enabled;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    
    #endregion
    
    #region 审计日志
    
    public async Task<PagedResponse<AuditLogDto>> GetAuditLogsAsync(AuditLogQueryRequest query, CancellationToken cancellationToken = default)
    {
        var actionStr = query.Action?.ToString();
        var (items, totalCount) = await _auditLogService.GetPagedAsync(
            query.Page,
            query.PageSize,
            query.UserId,
            actionStr,
            query.StartTime,
            query.EndTime,
            cancellationToken);
        
        var dtos = items.Select(MapToAuditLogDto).ToList();
        
        return new PagedResponse<AuditLogDto>
        {
            Items = dtos,
            Total = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
    
    #endregion
    
    #region Mapping Helpers
    
    private static UserDto MapToUserDto(User user)
    {
        return new UserDto(
            Id: user.Id,
            Username: user.Username,
            Email: user.Email,
            Avatar: user.Avatar,
            Bio: user.Bio,
            RealName: user.RealName,
            Organization: user.Organization,
            Status: user.Status,
            SolvedCount: user.SolvedCount,
            SubmissionCount: user.SubmissionCount,
            Rating: user.Rating,
            MaxRating: user.MaxRating,
            CreatedAt: user.CreatedAt,
            LastLoginAt: user.LastLoginAt,
            Roles: user.UserRoles?
                .Where(ur => ur.Role != null)
                .Select(ur => ur.Role!.Code)
                .Distinct()
                .ToList() ?? [],
            Permissions: []
        );
    }
    
    private static RoleDto MapToRoleDto(Role role)
    {
        var permissions = role.RolePermissions?
            .Select(rp => rp.Permission?.Code ?? "")
            .Where(c => !string.IsNullOrEmpty(c))
            .ToList() ?? [];
        
        return new RoleDto(
            Id: role.Id,
            Name: role.Name,
            Code: role.Code,
            Description: role.Description,
            IsSystem: role.IsSystem,
            Priority: role.Priority,
            Permissions: permissions
        );
    }
    
    private static PermissionDto MapToPermissionDto(Permission permission)
    {
        return new PermissionDto(
            Id: permission.Id,
            Code: permission.Code,
            Name: permission.Name,
            Description: permission.Description,
            Category: permission.Category
        );
    }
    
    private static SystemConfigDto MapToSystemConfigDto(SystemConfig config)
    {
        return new SystemConfigDto(
            Id: config.Id,
            Key: config.Key,
            Value: config.Value,
            Type: config.Type,
            Category: config.Category,
            Description: config.Description,
            IsPublic: config.IsPublic,
            UpdatedAt: config.UpdatedAt
        );
    }
    
    private static LanguageConfigDto MapToLanguageConfigDto(LanguageConfig config)
    {
        return new LanguageConfigDto(
            Id: config.Id,
            Code: config.Code,
            Name: config.Name,
            Version: config.Version,
            IsEnabled: config.IsEnabled,
            CompileCommand: config.CompileCommand,
            RunCommand: config.RunCommand,
            SourceFileName: config.SourceFileName,
            ExecutableFileName: config.ExecutableFileName,
            CompileTimeLimit: config.CompileTimeLimit,
            CompileMemoryLimit: config.CompileMemoryLimit,
            TimeMultiplier: config.TimeMultiplier,
            MemoryMultiplier: config.MemoryMultiplier,
            MonacoLanguage: config.MonacoLanguage,
            Template: config.Template,
            Order: config.Order
        );
    }
    
    private static JudgerStatusDto MapToJudgerStatusDto(JudgerNode node, JudgerNodeInfo? runtime)
    {
        var isOnline = runtime != null && string.Equals(runtime.Status, "Online", StringComparison.OrdinalIgnoreCase);

        return new JudgerStatusDto(
            Id: node.Id,
            JudgerId: node.Id.ToString(),
            Name: node.Name,
            Hostname: null,
            IsOnline: isOnline,
            IsEnabled: node.IsEnabled,
            CurrentTasks: runtime?.CurrentTasks ?? 0,
            MaxTasks: node.MaxConcurrentTasks,
            CpuUsage: null,
            MemoryUsage: null,
            CompletedTasks: 0,
            Version: null,
            LastHeartbeat: runtime?.LastHeartbeat,
            StartedAt: null
        );
    }
    
    private static AuditLogDto MapToAuditLogDto(AuditLog log)
    {
        return new AuditLogDto(
            Id: log.Id,
            UserId: log.UserId,
            Username: log.Username,
            Action: log.Action,
            EntityType: log.EntityType,
            EntityId: log.EntityId,
            Description: log.Description,
            IpAddress: log.IpAddress,
            Timestamp: log.Timestamp
        );
    }
    
    #endregion
}

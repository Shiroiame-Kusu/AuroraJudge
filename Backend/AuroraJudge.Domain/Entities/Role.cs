using AuroraJudge.Domain.Common;

namespace AuroraJudge.Domain.Entities;

/// <summary>
/// 角色实体
/// </summary>
public class Role : AuditableEntity
{
    /// <summary>角色名称</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>角色代码（唯一标识）</summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>角色描述</summary>
    public string? Description { get; set; }
    
    /// <summary>是否为系统角色（不可删除）</summary>
    public bool IsSystem { get; set; }
    
    /// <summary>排序优先级</summary>
    public int Priority { get; set; }
    
    // 导航属性
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

/// <summary>
/// 用户-角色关联
/// </summary>
public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime AssignedAt { get; set; }
    public Guid? AssignedBy { get; set; }
    
    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}

/// <summary>
/// 权限实体
/// </summary>
public class Permission : BaseEntity
{
    /// <summary>权限代码（如：problem.create）</summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>权限名称</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>权限描述</summary>
    public string? Description { get; set; }
    
    /// <summary>权限分类</summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>排序</summary>
    public int Order { get; set; }
    
    // 导航属性
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}

/// <summary>
/// 角色-权限关联
/// </summary>
public class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    
    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}

/// <summary>
/// 用户-权限直接关联（用于特殊授权）
/// </summary>
public class UserPermission
{
    public Guid UserId { get; set; }
    public Guid PermissionId { get; set; }
    
    /// <summary>是否为拒绝权限（用于覆盖角色权限）</summary>
    public bool IsDenied { get; set; }
    
    public DateTime GrantedAt { get; set; }
    public Guid? GrantedBy { get; set; }
    
    /// <summary>权限过期时间（null表示永久）</summary>
    public DateTime? ExpiresAt { get; set; }
    
    public virtual User User { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}

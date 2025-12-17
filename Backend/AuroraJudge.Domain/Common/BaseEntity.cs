namespace AuroraJudge.Domain.Common;

/// <summary>
/// 实体基类
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
}

/// <summary>
/// 可审计实体基类
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

/// <summary>
/// 软删除实体基类
/// </summary>
public abstract class SoftDeletableEntity : AuditableEntity
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}

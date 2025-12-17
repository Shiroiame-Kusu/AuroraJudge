using AuroraJudge.Domain.Common;
using AuroraJudge.Domain.Enums;

namespace AuroraJudge.Domain.Entities;

/// <summary>
/// 全局公告
/// </summary>
public class Announcement : SoftDeletableEntity
{
    /// <summary>公告标题</summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>公告内容（Markdown）</summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>公告状态</summary>
    public AnnouncementStatus Status { get; set; } = AnnouncementStatus.Draft;
    
    /// <summary>是否置顶</summary>
    public bool IsPinned { get; set; }
    
    /// <summary>置顶排序</summary>
    public int PinOrder { get; set; }
    
    /// <summary>发布时间</summary>
    public DateTime? PublishedAt { get; set; }
    
    /// <summary>浏览次数</summary>
    public int ViewCount { get; set; }
    
    public Guid AuthorId { get; set; }
    public virtual User Author { get; set; } = null!;
}

/// <summary>
/// 审计日志
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>操作用户ID</summary>
    public Guid? UserId { get; set; }
    
    /// <summary>操作用户名</summary>
    public string? Username { get; set; }
    
    /// <summary>操作类型</summary>
    public AuditAction Action { get; set; }
    
    /// <summary>目标实体类型</summary>
    public string? EntityType { get; set; }
    
    /// <summary>目标实体ID</summary>
    public string? EntityId { get; set; }
    
    /// <summary>操作描述</summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>旧值（JSON）</summary>
    public string? OldValue { get; set; }
    
    /// <summary>新值（JSON）</summary>
    public string? NewValue { get; set; }
    
    /// <summary>IP地址</summary>
    public string? IpAddress { get; set; }
    
    /// <summary>User Agent</summary>
    public string? UserAgent { get; set; }
    
    /// <summary>操作时间</summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>额外数据（JSON）</summary>
    public string? ExtraData { get; set; }
}

/// <summary>
/// 系统配置
/// </summary>
public class SystemConfig : BaseEntity
{
    /// <summary>配置键</summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>配置值</summary>
    public string Value { get; set; } = string.Empty;
    
    /// <summary>配置类型（string/int/bool/json）</summary>
    public string Type { get; set; } = "string";
    
    /// <summary>配置分类</summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>配置描述</summary>
    public string? Description { get; set; }
    
    /// <summary>是否为公开配置（前端可读）</summary>
    public bool IsPublic { get; set; }
    
    /// <summary>最后更新时间</summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>最后更新者</summary>
    public Guid? UpdatedBy { get; set; }
}

/// <summary>
/// 编程语言配置
/// </summary>
public class LanguageConfig : BaseEntity
{
    /// <summary>语言代码（如：cpp, java, python3）</summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>显示名称</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>版本信息</summary>
    public string? Version { get; set; }
    
    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>编译命令模板</summary>
    public string? CompileCommand { get; set; }
    
    /// <summary>运行命令模板</summary>
    public string RunCommand { get; set; } = string.Empty;
    
    /// <summary>源文件名</summary>
    public string SourceFileName { get; set; } = string.Empty;
    
    /// <summary>可执行文件名</summary>
    public string? ExecutableFileName { get; set; }
    
    /// <summary>编译时间限制（毫秒）</summary>
    public int CompileTimeLimit { get; set; } = 30000;
    
    /// <summary>编译内存限制（KB）</summary>
    public int CompileMemoryLimit { get; set; } = 524288;
    
    /// <summary>时间系数（用于补偿解释型语言）</summary>
    public double TimeMultiplier { get; set; } = 1.0;
    
    /// <summary>内存系数</summary>
    public double MemoryMultiplier { get; set; } = 1.0;
    
    /// <summary>Monaco Editor 语言ID</summary>
    public string? MonacoLanguage { get; set; }
    
    /// <summary>代码模板</summary>
    public string? Template { get; set; }
    
    /// <summary>排序</summary>
    public int Order { get; set; }
}

/// <summary>
/// 判题机节点配置（持久化存储）
/// </summary>
public class JudgerNode : SoftDeletableEntity
{
    /// <summary>判题机名称</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>判题机描述</summary>
    public string? Description { get; set; }
    
    /// <summary>密钥哈希</summary>
    public string SecretHash { get; set; } = string.Empty;
    
    /// <summary>最大并发任务数</summary>
    public int MaxConcurrentTasks { get; set; } = 4;
    
    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>支持的语言（逗号分隔，空表示全部）</summary>
    public string? SupportedLanguages { get; set; }
    
    /// <summary>最后连接时间</summary>
    public DateTime? LastConnectedAt { get; set; }
    
    /// <summary>最后连接IP</summary>
    public string? LastConnectedIp { get; set; }
}

/// <summary>
/// 判题机状态
/// </summary>
public class JudgerStatus : BaseEntity
{
    /// <summary>判题机唯一标识</summary>
    public string JudgerId { get; set; } = string.Empty;
    
    /// <summary>判题机名称</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>主机名/IP</summary>
    public string? Hostname { get; set; }
    
    /// <summary>是否在线</summary>
    public bool IsOnline { get; set; }
    
    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>当前任务数</summary>
    public int CurrentTasks { get; set; }
    
    /// <summary>最大并发任务数</summary>
    public int MaxTasks { get; set; }
    
    /// <summary>CPU 使用率</summary>
    public double? CpuUsage { get; set; }
    
    /// <summary>内存使用率</summary>
    public double? MemoryUsage { get; set; }
    
    /// <summary>已完成任务数</summary>
    public long CompletedTasks { get; set; }
    
    /// <summary>版本信息</summary>
    public string? Version { get; set; }
    
    /// <summary>最后心跳时间</summary>
    public DateTime? LastHeartbeat { get; set; }
    
    /// <summary>启动时间</summary>
    public DateTime? StartedAt { get; set; }
}

/// <summary>
/// 功能开关
/// </summary>
public class FeatureFlag : BaseEntity
{
    /// <summary>功能代码</summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>功能名称</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>功能描述</summary>
    public string? Description { get; set; }
    
    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; }
    
    /// <summary>启用条件（JSON，用于灰度发布）</summary>
    public string? Conditions { get; set; }
    
    /// <summary>最后更新时间</summary>
    public DateTime UpdatedAt { get; set; }
}

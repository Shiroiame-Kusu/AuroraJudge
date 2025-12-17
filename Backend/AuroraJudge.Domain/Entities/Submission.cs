using AuroraJudge.Domain.Common;
using AuroraJudge.Domain.Enums;

namespace AuroraJudge.Domain.Entities;

/// <summary>
/// 提交记录
/// </summary>
public class Submission : AuditableEntity
{
    public Guid ProblemId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ContestId { get; set; }
    
    /// <summary>提交的代码</summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>编程语言</summary>
    public string Language { get; set; } = string.Empty;
    
    /// <summary>代码长度（字节）</summary>
    public int CodeLength { get; set; }
    
    /// <summary>提交IP</summary>
    public string? SubmitIp { get; set; }
    
    // 评测结果
    /// <summary>评测状态</summary>
    public JudgeStatus Status { get; set; } = JudgeStatus.Pending;
    
    /// <summary>得分（OI模式）</summary>
    public int? Score { get; set; }
    
    /// <summary>最大运行时间（毫秒）</summary>
    public int? TimeUsed { get; set; }
    
    /// <summary>最大内存使用（KB）</summary>
    public int? MemoryUsed { get; set; }
    
    /// <summary>编译信息</summary>
    public string? CompileMessage { get; set; }
    
    /// <summary>评测信息</summary>
    public string? JudgeMessage { get; set; }
    
    /// <summary>评测完成时间</summary>
    public DateTime? JudgedAt { get; set; }
    
    /// <summary>评测机ID</summary>
    public string? JudgerId { get; set; }
    
    /// <summary>是否为赛后提交</summary>
    public bool IsAfterContest { get; set; }
    
    /// <summary>代码共享设置：0-私有，1-通过后公开，2-公开</summary>
    public int ShareStatus { get; set; }
    
    // 导航属性
    public virtual Problem Problem { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual Contest? Contest { get; set; }
    public virtual ICollection<JudgeResult> JudgeResults { get; set; } = new List<JudgeResult>();
}

/// <summary>
/// 单个测试点的评测结果
/// </summary>
public class JudgeResult : BaseEntity
{
    public Guid SubmissionId { get; set; }
    
    /// <summary>测试点序号</summary>
    public int TestCaseOrder { get; set; }
    
    /// <summary>Subtask 序号</summary>
    public int? Subtask { get; set; }
    
    /// <summary>评测状态</summary>
    public JudgeStatus Status { get; set; }
    
    /// <summary>运行时间（毫秒）</summary>
    public int TimeUsed { get; set; }
    
    /// <summary>内存使用（KB）</summary>
    public int MemoryUsed { get; set; }
    
    /// <summary>得分</summary>
    public int Score { get; set; }
    
    /// <summary>退出代码</summary>
    public int? ExitCode { get; set; }
    
    /// <summary>评测信息</summary>
    public string? Message { get; set; }
    
    /// <summary>Checker 输出</summary>
    public string? CheckerOutput { get; set; }
    
    public virtual Submission Submission { get; set; } = null!;
}

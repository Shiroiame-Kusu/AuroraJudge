using AuroraJudge.Domain.Common;
using AuroraJudge.Domain.Enums;

namespace AuroraJudge.Domain.Entities;

/// <summary>
/// 比赛/作业
/// </summary>
public class Contest : SoftDeletableEntity
{
    /// <summary>比赛标题</summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>比赛描述（Markdown）</summary>
    public string? Description { get; set; }
    
    /// <summary>开始时间</summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>结束时间</summary>
    public DateTime EndTime { get; set; }
    
    /// <summary>封榜时间（null表示不封榜）</summary>
    public DateTime? FreezeTime { get; set; }
    
    /// <summary>解封时间（null表示比赛结束后解封）</summary>
    public DateTime? UnfreezeTime { get; set; }
    
    /// <summary>比赛类型</summary>
    public ContestType Type { get; set; } = ContestType.ACM;
    
    /// <summary>可见性</summary>
    public ContestVisibility Visibility { get; set; } = ContestVisibility.Public;
    
    /// <summary>比赛密码（仅 Protected 模式）</summary>
    public string? Password { get; set; }
    
    /// <summary>是否计入 Rating</summary>
    public bool IsRated { get; set; }
    
    /// <summary>Rating 下限（null表示无限制）</summary>
    public int? RatingFloor { get; set; }
    
    /// <summary>Rating 上限</summary>
    public int? RatingCeiling { get; set; }
    
    /// <summary>允许迟交</summary>
    public bool AllowLateSubmission { get; set; }
    
    /// <summary>迟交惩罚（每分钟扣分比例）</summary>
    public double LateSubmissionPenalty { get; set; }
    
    /// <summary>是否显示排行榜</summary>
    public bool ShowRanking { get; set; } = true;
    
    /// <summary>是否允许查看他人代码</summary>
    public bool AllowViewOthersCode { get; set; }
    
    /// <summary>结束后是否公开题目</summary>
    public bool PublishProblemsAfterEnd { get; set; } = true;
    
    /// <summary>最大参赛人数（null表示无限制）</summary>
    public int? MaxParticipants { get; set; }
    
    /// <summary>规则说明</summary>
    public string? Rules { get; set; }
    
    // 关系
    public Guid CreatorId { get; set; }
    public virtual User Creator { get; set; } = null!;
    
    public virtual ICollection<ContestProblem> ContestProblems { get; set; } = new List<ContestProblem>();
    public virtual ICollection<ContestParticipant> Participants { get; set; } = new List<ContestParticipant>();
    public virtual ICollection<ContestAnnouncement> Announcements { get; set; } = new List<ContestAnnouncement>();
    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}

/// <summary>
/// 比赛-题目关联
/// </summary>
public class ContestProblem
{
    public Guid ContestId { get; set; }
    public Guid ProblemId { get; set; }
    
    /// <summary>题目标签（A, B, C...）</summary>
    public string Label { get; set; } = string.Empty;
    
    /// <summary>排序</summary>
    public int Order { get; set; }
    
    /// <summary>题目分值（OI模式）</summary>
    public int? Score { get; set; }
    
    /// <summary>题目颜色（用于排行榜显示）</summary>
    public string? Color { get; set; }
    
    /// <summary>提交数</summary>
    public int SubmissionCount { get; set; }
    
    /// <summary>通过数</summary>
    public int AcceptedCount { get; set; }
    
    /// <summary>首次通过时间</summary>
    public DateTime? FirstAcceptedAt { get; set; }
    
    /// <summary>首次通过用户</summary>
    public Guid? FirstAcceptedBy { get; set; }
    
    public virtual Contest Contest { get; set; } = null!;
    public virtual Problem Problem { get; set; } = null!;
}

/// <summary>
/// 比赛参与者
/// </summary>
public class ContestParticipant
{
    public Guid ContestId { get; set; }
    public Guid UserId { get; set; }
    
    /// <summary>报名时间</summary>
    public DateTime RegisteredAt { get; set; }
    
    /// <summary>参赛状态：0-已报名，1-已参赛，2-已取消</summary>
    public int Status { get; set; }
    
    /// <summary>是否为虚拟参赛</summary>
    public bool IsVirtual { get; set; }
    
    /// <summary>虚拟参赛开始时间</summary>
    public DateTime? VirtualStartTime { get; set; }
    
    /// <summary>总得分</summary>
    public int Score { get; set; }
    
    /// <summary>罚时（ACM模式，秒）</summary>
    public int Penalty { get; set; }
    
    /// <summary>排名</summary>
    public int? Rank { get; set; }
    
    /// <summary>Rating 变化</summary>
    public int? RatingChange { get; set; }
    
    public virtual Contest Contest { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// 比赛公告
/// </summary>
public class ContestAnnouncement : AuditableEntity
{
    public Guid ContestId { get; set; }
    
    /// <summary>公告标题</summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>公告内容</summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>是否置顶</summary>
    public bool IsPinned { get; set; }
    
    /// <summary>关联题目ID</summary>
    public Guid? ProblemId { get; set; }
    
    public virtual Contest Contest { get; set; } = null!;
}

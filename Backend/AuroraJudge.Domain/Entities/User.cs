using AuroraJudge.Domain.Common;
using AuroraJudge.Domain.Enums;

namespace AuroraJudge.Domain.Entities;

/// <summary>
/// 用户实体
/// </summary>
public class User : SoftDeletableEntity
{
    /// <summary>用户名</summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>邮箱</summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>密码哈希</summary>
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>邮箱是否已验证</summary>
    public bool EmailConfirmed { get; set; }
    
    /// <summary>头像URL</summary>
    public string? Avatar { get; set; }
    
    /// <summary>个人简介</summary>
    public string? Bio { get; set; }
    
    /// <summary>真实姓名</summary>
    public string? RealName { get; set; }
    
    /// <summary>学校/组织</summary>
    public string? Organization { get; set; }
    
    /// <summary>用户状态</summary>
    public UserStatus Status { get; set; } = UserStatus.Active;
    
    /// <summary>最后登录时间</summary>
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>最后登录IP</summary>
    public string? LastLoginIp { get; set; }
    
    /// <summary>登录失败次数</summary>
    public int FailedLoginAttempts { get; set; }
    
    /// <summary>锁定结束时间</summary>
    public DateTime? LockoutEnd { get; set; }
    
    /// <summary>显示名称</summary>
    public string? DisplayName { get; set; }
    
    /// <summary>刷新令牌</summary>
    public string? RefreshToken { get; set; }
    
    /// <summary>刷新令牌过期时间</summary>
    public DateTime? RefreshTokenExpiryTime { get; set; }
    
    // 统计信息
    /// <summary>已解决题目数</summary>
    public int SolvedCount { get; set; }
    
    /// <summary>提交总数</summary>
    public int SubmissionCount { get; set; }
    
    /// <summary>Rating 分数</summary>
    public int Rating { get; set; } = 1500;
    
    /// <summary>最高 Rating</summary>
    public int MaxRating { get; set; } = 1500;
    
    // 导航属性
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public virtual ICollection<Problem> CreatedProblems { get; set; } = new List<Problem>();
    public virtual ICollection<Contest> CreatedContests { get; set; } = new List<Contest>();
    public virtual ICollection<ContestParticipant> ContestParticipations { get; set; } = new List<ContestParticipant>();
    public virtual ICollection<UserSolvedProblem> SolvedProblems { get; set; } = new List<UserSolvedProblem>();
}

/// <summary>
/// 用户已解决题目记录
/// </summary>
public class UserSolvedProblem
{
    public Guid UserId { get; set; }
    public Guid ProblemId { get; set; }
    public Guid FirstAcceptedSubmissionId { get; set; }
    public DateTime SolvedAt { get; set; }
    
    public virtual User User { get; set; } = null!;
    public virtual Problem Problem { get; set; } = null!;
}

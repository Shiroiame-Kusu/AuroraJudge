using AuroraJudge.Domain.Common;
using AuroraJudge.Domain.Enums;

namespace AuroraJudge.Domain.Entities;

/// <summary>
/// 题目实体
/// </summary>
public class Problem : SoftDeletableEntity
{
    /// <summary>题目标题</summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>题目描述（Markdown）</summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>输入格式说明</summary>
    public string InputFormat { get; set; } = string.Empty;
    
    /// <summary>输出格式说明</summary>
    public string OutputFormat { get; set; } = string.Empty;
    
    /// <summary>样例输入</summary>
    public string? SampleInput { get; set; }
    
    /// <summary>样例输出</summary>
    public string? SampleOutput { get; set; }
    
    /// <summary>提示</summary>
    public string? Hint { get; set; }
    
    /// <summary>来源</summary>
    public string? Source { get; set; }
    
    // 限制设置
    /// <summary>时间限制（毫秒）</summary>
    public int TimeLimit { get; set; } = 1000;
    
    /// <summary>内存限制（KB）</summary>
    public int MemoryLimit { get; set; } = 262144;
    
    /// <summary>栈空间限制（KB）</summary>
    public int StackLimit { get; set; } = 65536;
    
    /// <summary>输出限制（KB）</summary>
    public int OutputLimit { get; set; } = 65536;
    
    // 评测配置
    /// <summary>评测模式</summary>
    public JudgeMode JudgeMode { get; set; } = JudgeMode.Standard;
    
    /// <summary>Special Judge 代码</summary>
    public string? SpecialJudgeCode { get; set; }
    
    /// <summary>Special Judge 语言</summary>
    public string? SpecialJudgeLanguage { get; set; }
    
    /// <summary>交互器代码</summary>
    public string? InteractorCode { get; set; }
    
    /// <summary>交互器语言</summary>
    public string? InteractorLanguage { get; set; }
    
    /// <summary>允许的编程语言（null表示允许所有）</summary>
    public string? AllowedLanguages { get; set; }
    
    // 状态
    /// <summary>可见性</summary>
    public ProblemVisibility Visibility { get; set; } = ProblemVisibility.Private;
    
    /// <summary>难度</summary>
    public ProblemDifficulty Difficulty { get; set; } = ProblemDifficulty.Unrated;
    
    // 统计
    /// <summary>提交数</summary>
    public int SubmissionCount { get; set; }
    
    /// <summary>通过数</summary>
    public int AcceptedCount { get; set; }
    
    // 关系
    public Guid CreatorId { get; set; }
    public virtual User Creator { get; set; } = null!;
    
    public virtual ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
    public virtual ICollection<ProblemTag> ProblemTags { get; set; } = new List<ProblemTag>();
    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public virtual ICollection<ContestProblem> ContestProblems { get; set; } = new List<ContestProblem>();
    public virtual ICollection<UserSolvedProblem> SolvedByUsers { get; set; } = new List<UserSolvedProblem>();
}

/// <summary>
/// 测试用例
/// </summary>
public class TestCase : BaseEntity
{
    public Guid ProblemId { get; set; }
    
    /// <summary>测试点序号</summary>
    public int Order { get; set; }
    
    /// <summary>输入文件路径</summary>
    public string InputPath { get; set; } = string.Empty;
    
    /// <summary>输出文件路径</summary>
    public string OutputPath { get; set; } = string.Empty;
    
    /// <summary>输入文件大小（字节）</summary>
    public long InputSize { get; set; }
    
    /// <summary>输出文件大小（字节）</summary>
    public long OutputSize { get; set; }
    
    /// <summary>分值（用于部分分）</summary>
    public int Score { get; set; } = 10;
    
    /// <summary>是否为样例</summary>
    public bool IsSample { get; set; }
    
    /// <summary>测试点分组（用于 Subtask）</summary>
    public int? Subtask { get; set; }
    
    /// <summary>描述/备注</summary>
    public string? Description { get; set; }
    
    public virtual Problem Problem { get; set; } = null!;
}

/// <summary>
/// 标签
/// </summary>
public class Tag : BaseEntity
{
    /// <summary>标签名称</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>标签颜色</summary>
    public string? Color { get; set; }
    
    /// <summary>标签分类</summary>
    public string? Category { get; set; }
    
    /// <summary>使用次数</summary>
    public int UsageCount { get; set; }
    
    public virtual ICollection<ProblemTag> ProblemTags { get; set; } = new List<ProblemTag>();
}

/// <summary>
/// 题目-标签关联
/// </summary>
public class ProblemTag
{
    public Guid ProblemId { get; set; }
    public Guid TagId { get; set; }
    
    public virtual Problem Problem { get; set; } = null!;
    public virtual Tag Tag { get; set; } = null!;
}

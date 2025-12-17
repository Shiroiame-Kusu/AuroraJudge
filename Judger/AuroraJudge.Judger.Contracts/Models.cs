namespace AuroraJudge.Judger.Contracts;

/// <summary>
/// 判题任务
/// </summary>
public class JudgeTask
{
    public Guid SubmissionId { get; set; }
    public Guid ProblemId { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int TimeLimit { get; set; }
    public int MemoryLimit { get; set; }
    public JudgeMode JudgeMode { get; set; }
    public string? SpecialJudgeCode { get; set; }
    public IReadOnlyList<TestCaseData> TestCases { get; set; } = [];
}

/// <summary>
/// 测试用例数据
/// </summary>
public class TestCaseData
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public string InputPath { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public int Score { get; set; }
}

/// <summary>
/// 判题结果
/// </summary>
public class JudgeResponse
{
    public Guid SubmissionId { get; set; }
    public JudgeStatus Status { get; set; }
    public int? Time { get; set; }
    public int? Memory { get; set; }
    public int? Score { get; set; }
    public string? CompileInfo { get; set; }
    public IReadOnlyList<TestCaseResult> Results { get; set; } = [];
}

/// <summary>
/// 单个测试点结果
/// </summary>
public class TestCaseResult
{
    public int Order { get; set; }
    public JudgeStatus Status { get; set; }
    public int Time { get; set; }
    public int Memory { get; set; }
    public int Score { get; set; }
    public string? Message { get; set; }
    public string? ExitCode { get; set; }
}

/// <summary>
/// 判题状态
/// </summary>
public enum JudgeStatus
{
    Pending = 0,
    Judging = 1,
    Accepted = 2,
    WrongAnswer = 3,
    TimeLimitExceeded = 4,
    MemoryLimitExceeded = 5,
    RuntimeError = 6,
    CompileError = 7,
    OutputLimitExceeded = 8,
    PresentationError = 9,
    SystemError = 10,
    PartiallyAccepted = 11
}

/// <summary>
/// 判题模式
/// </summary>
public enum JudgeMode
{
    Standard = 0,
    SpecialJudge = 1,
    Interactive = 2
}

/// <summary>
/// 语言配置
/// </summary>
public class LanguageConfig
{
    public string Name { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string? CompileCommand { get; set; }
    public string RunCommand { get; set; } = string.Empty;
    public double TimeMultiplier { get; set; } = 1.0;
    public double MemoryMultiplier { get; set; } = 1.0;
}

/// <summary>
/// 判题机心跳
/// </summary>
public class JudgerHeartbeat
{
    public Guid JudgerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public int CurrentTasks { get; set; }
    public int MaxConcurrentTasks { get; set; }
    public DateTime Timestamp { get; set; }
}

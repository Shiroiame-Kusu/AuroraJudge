namespace AuroraJudge.Domain.Enums;

/// <summary>
/// 评测状态
/// </summary>
public enum JudgeStatus
{
    /// <summary>待评测</summary>
    Pending = 0,
    
    /// <summary>评测中</summary>
    Judging = 1,
    
    /// <summary>编译中</summary>
    Compiling = 2,
    
    /// <summary>运行中</summary>
    Running = 3,
    
    /// <summary>通过</summary>
    Accepted = 10,
    
    /// <summary>答案错误</summary>
    WrongAnswer = 11,
    
    /// <summary>时间超限</summary>
    TimeLimitExceeded = 12,
    
    /// <summary>内存超限</summary>
    MemoryLimitExceeded = 13,
    
    /// <summary>输出超限</summary>
    OutputLimitExceeded = 14,
    
    /// <summary>运行时错误</summary>
    RuntimeError = 15,
    
    /// <summary>编译错误</summary>
    CompileError = 16,
    
    /// <summary>格式错误</summary>
    PresentationError = 17,
    
    /// <summary>系统错误</summary>
    SystemError = 20,
    
    /// <summary>部分通过</summary>
    PartiallyAccepted = 21,
    
    /// <summary>跳过</summary>
    Skipped = 22
}

/// <summary>
/// 用户状态
/// </summary>
public enum UserStatus
{
    /// <summary>正常</summary>
    Active = 0,
    
    /// <summary>未激活</summary>
    Inactive = 1,
    
    /// <summary>已禁用</summary>
    Banned = 2,
    
    /// <summary>已删除</summary>
    Deleted = 3
}

/// <summary>
/// 题目可见性
/// </summary>
public enum ProblemVisibility
{
    /// <summary>公开</summary>
    Public = 0,
    
    /// <summary>私有</summary>
    Private = 1,
    
    /// <summary>仅比赛可见</summary>
    ContestOnly = 2,
    
    /// <summary>隐藏</summary>
    Hidden = 3
}

/// <summary>
/// 题目难度
/// </summary>
public enum ProblemDifficulty
{
    /// <summary>未评级</summary>
    Unrated = 0,
    
    /// <summary>简单</summary>
    Easy = 1,
    
    /// <summary>中等</summary>
    Medium = 2,
    
    /// <summary>困难</summary>
    Hard = 3,
    
    /// <summary>专家</summary>
    Expert = 4
}

/// <summary>
/// 评测模式
/// </summary>
public enum JudgeMode
{
    /// <summary>标准模式：直接比较输出</summary>
    Standard = 0,
    
    /// <summary>特殊评测：使用自定义 Checker</summary>
    SpecialJudge = 1,
    
    /// <summary>交互式评测</summary>
    Interactive = 2,
    
    /// <summary>文件比较：忽略行尾空白</summary>
    FileComparison = 3
}

/// <summary>
/// 比赛类型
/// </summary>
public enum ContestType
{
    /// <summary>ACM/ICPC 赛制</summary>
    ACM = 0,
    
    /// <summary>OI 赛制（部分分）</summary>
    OI = 1,
    
    /// <summary>IOI 赛制（即时反馈）</summary>
    IOI = 2,
    
    /// <summary>乐多赛制</summary>
    LeDuo = 3,
    
    /// <summary>作业模式</summary>
    Homework = 4
}

/// <summary>
/// 比赛可见性
/// </summary>
public enum ContestVisibility
{
    /// <summary>公开</summary>
    Public = 0,
    
    /// <summary>需要密码</summary>
    Protected = 1,
    
    /// <summary>私有（仅邀请）</summary>
    Private = 2
}

/// <summary>
/// 比赛状态
/// </summary>
public enum ContestStatus
{
    /// <summary>未开始</summary>
    Pending = 0,
    
    /// <summary>进行中</summary>
    Running = 1,
    
    /// <summary>已封榜</summary>
    Frozen = 2,
    
    /// <summary>已结束</summary>
    Ended = 3
}

/// <summary>
/// 公告状态
/// </summary>
public enum AnnouncementStatus
{
    /// <summary>草稿</summary>
    Draft = 0,
    
    /// <summary>已发布</summary>
    Published = 1,
    
    /// <summary>已置顶</summary>
    Pinned = 2,
    
    /// <summary>已归档</summary>
    Archived = 3
}

/// <summary>
/// 审计操作类型
/// </summary>
public enum AuditAction
{
    /// <summary>创建</summary>
    Create = 0,
    
    /// <summary>更新</summary>
    Update = 1,
    
    /// <summary>删除</summary>
    Delete = 2,
    
    /// <summary>登录</summary>
    Login = 3,
    
    /// <summary>登出</summary>
    Logout = 4,
    
    /// <summary>提交代码</summary>
    Submit = 5,
    
    /// <summary>重判</summary>
    Rejudge = 6,
    
    /// <summary>权限变更</summary>
    PermissionChange = 7
}

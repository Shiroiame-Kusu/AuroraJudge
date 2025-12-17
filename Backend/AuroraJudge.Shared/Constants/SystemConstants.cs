namespace AuroraJudge.Shared.Constants;

/// <summary>
/// 缓存键常量
/// </summary>
public static class CacheKeys
{
    private const string UserPermissionsTemplate = "user:permissions:{0}";
    private const string UserRolesTemplate = "user:roles:{0}";
    private const string UserProfileTemplate = "user:profile:{0}";
    private const string TokenBlacklistTemplate = "token:blacklist:{0}";
    private const string SubmitRateLimitTemplate = "ratelimit:submit:{0}";
    
    public const string Problem = "problem:{0}";
    public const string ProblemList = "problems:list:{0}:{1}:{2}";
    public const string ProblemTestCases = "problem:testcases:{0}";
    
    public const string Contest = "contest:{0}";
    public const string ContestRanking = "contest:ranking:{0}";
    public const string ContestProblems = "contest:problems:{0}";
    
    public const string SystemConfig = "system:config:{0}";
    public const string SystemConfigs = "system:configs:all";
    public const string LanguageConfigs = "system:languages:all";
    public const string FeatureFlags = "system:features:all";
    
    public const string JudgerStatus = "judger:status:{0}";
    public const string JudgerStatuses = "judger:statuses:all";
    
    public const string Leaderboard = "leaderboard:{0}:{1}";
    
    public static string UserPermissions(Guid userId) => string.Format(UserPermissionsTemplate, userId);
    public static string UserRoles(Guid userId) => string.Format(UserRolesTemplate, userId);
    public static string UserProfile(Guid userId) => string.Format(UserProfileTemplate, userId);
    public static string TokenBlacklist(string token) => string.Format(TokenBlacklistTemplate, token);
    public static string SubmitRateLimit(Guid userId) => string.Format(SubmitRateLimitTemplate, userId);
    
    public static string Format(string template, params object[] args) => string.Format(template, args);
}

/// <summary>
/// 消息队列名称
/// </summary>
public static class QueueNames
{
    public const string JudgeTask = "judge.task";
    public const string JudgeResult = "judge.result";
    public const string JudgeHeartbeat = "judge.heartbeat";
    public const string Notification = "notification";
    public const string Email = "email";
}

/// <summary>
/// 系统配置键
/// </summary>
public static class ConfigKeys
{
    // 站点配置
    public const string SiteName = "site.name";
    public const string SiteDescription = "site.description";
    public const string SiteKeywords = "site.keywords";
    public const string SiteLogo = "site.logo";
    public const string SiteFavicon = "site.favicon";
    public const string SiteFooter = "site.footer";
    
    // 用户配置
    public const string AllowRegistration = "user.allow_registration";
    public const string RequireEmailVerification = "user.require_email_verification";
    public const string DefaultRole = "user.default_role";
    public const string MaxLoginAttempts = "user.max_login_attempts";
    public const string LockoutDuration = "user.lockout_duration";
    
    // 提交配置
    public const string SubmissionRateLimit = "submission.rate_limit";
    public const string MaxCodeLength = "submission.max_code_length";
    public const string DefaultTimeLimit = "submission.default_time_limit";
    public const string DefaultMemoryLimit = "submission.default_memory_limit";
    
    // 判题配置
    public const string JudgeParallelism = "judge.parallelism";
    public const string JudgeTimeout = "judge.timeout";
    public const string JudgeRetryCount = "judge.retry_count";
    
    // 存储配置
    public const string StorageType = "storage.type";
    public const string StorageEndpoint = "storage.endpoint";
    public const string StorageBucket = "storage.bucket";
}

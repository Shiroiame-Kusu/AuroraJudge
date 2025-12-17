using AuroraJudge.Domain.Common;
using AuroraJudge.Domain.Entities;
using AuroraJudge.Domain.Enums;

namespace AuroraJudge.Domain.Interfaces;

/// <summary>
/// 用户仓储接口
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default);
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search = null, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetLeaderboardPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    
    // 角色管理
    Task<IReadOnlyList<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default);
    Task<Role?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddRoleAsync(Role role, CancellationToken cancellationToken = default);
    Task DeleteRoleAsync(Guid id, CancellationToken cancellationToken = default);
    Task AssignPermissionToRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
    Task ClearRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);
    
    // 权限管理
    Task<IReadOnlyList<Permission>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);
    Task<Permission?> GetPermissionByCodeAsync(string code, CancellationToken cancellationToken = default);
    
    // 系统配置
    Task<IReadOnlyList<SystemConfig>> GetSystemConfigsAsync(CancellationToken cancellationToken = default);
    Task<SystemConfig?> GetSystemConfigByKeyAsync(string key, CancellationToken cancellationToken = default);
    
    // 语言配置
    Task<IReadOnlyList<LanguageConfig>> GetLanguageConfigsAsync(CancellationToken cancellationToken = default);
    Task AddLanguageConfigAsync(LanguageConfig config, CancellationToken cancellationToken = default);
    
    // 判题机
    Task<IReadOnlyList<JudgerStatus>> GetJudgerStatusesAsync(CancellationToken cancellationToken = default);
    Task<JudgerStatus?> GetJudgerStatusByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JudgerNode>> GetJudgerNodesAsync(CancellationToken cancellationToken = default);
    Task<JudgerNode?> GetJudgerNodeByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// 题目仓储接口
/// </summary>
public interface IProblemRepository
{
    Task<Problem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Problem?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Problem> Items, int TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        string? search = null,
        Guid? tagId = null,
        ProblemDifficulty? difficulty = null,
        ProblemVisibility? visibility = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取对指定用户可见的题目列表（用于题库分页）。
    /// - 管理员/具备查看隐藏权限：可见全部
    /// - 普通用户：可见公开题目 + 自己创建的题目
    /// </summary>
    Task<(IReadOnlyList<Problem> Items, int TotalCount)> GetPagedForViewerAsync(
        int page,
        int pageSize,
        string? search,
        Guid? tagId,
        ProblemDifficulty? difficulty,
        Guid? viewerId,
        bool canViewHidden,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<Guid>> GetSolvedProblemIdsAsync(
        Guid userId,
        IReadOnlyList<Guid> problemIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Tag>> GetAllTagsAsync(CancellationToken cancellationToken = default);
    Task<Tag?> GetTagByIdAsync(Guid tagId, CancellationToken cancellationToken = default);
    Task<Tag?> GetTagByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddTagAsync(Tag tag, CancellationToken cancellationToken = default);
    Task DeleteTagAsync(Guid tagId, CancellationToken cancellationToken = default);
    Task AddAsync(Problem problem, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TestCase>> GetTestCasesAsync(Guid problemId, CancellationToken cancellationToken = default);
    Task<TestCase?> GetTestCaseByIdAsync(Guid problemId, Guid testCaseId, CancellationToken cancellationToken = default);
    Task AddTestCaseAsync(TestCase testCase, CancellationToken cancellationToken = default);
    Task DeleteTestCaseAsync(Guid problemId, Guid testCaseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetSubmissionIdsAsync(Guid problemId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 提交仓储接口
/// </summary>
public interface ISubmissionRepository
{
    Task<Submission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Submission?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Submission> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? userId = null,
        Guid? problemId = null,
        Guid? contestId = null,
        string? language = null,
        JudgeStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Submission>> GetRecentForSimilarityAsync(
        Guid problemId,
        string language,
        Guid excludeSubmissionId,
        int limit,
        CancellationToken cancellationToken = default);
    Task AddAsync(Submission submission, CancellationToken cancellationToken = default);
}

/// <summary>
/// 比赛仓储接口
/// </summary>
public interface IContestRepository
{
    Task<Contest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Contest?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Contest> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        ContestStatus? status = null,
        ContestType? type = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(Contest contest, CancellationToken cancellationToken = default);
    Task<bool> IsUserRegisteredAsync(Guid contestId, Guid userId, CancellationToken cancellationToken = default);
    Task AddParticipantAsync(ContestParticipant participant, CancellationToken cancellationToken = default);
    Task RemoveParticipantAsync(Guid contestId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContestParticipant>> GetStandingsAsync(Guid contestId, CancellationToken cancellationToken = default);
    Task ClearProblemsAsync(Guid contestId, CancellationToken cancellationToken = default);
    Task AddProblemAsync(ContestProblem problem, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Announcement>> GetAnnouncementsAsync(Guid contestId, CancellationToken cancellationToken = default);
    Task AddAnnouncementAsync(Announcement announcement, CancellationToken cancellationToken = default);
}

/// <summary>
/// 工作单元接口
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

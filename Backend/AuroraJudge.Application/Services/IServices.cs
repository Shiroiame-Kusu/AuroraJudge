using AuroraJudge.Application.DTOs;
using AuroraJudge.Application.DTOs.Auth;
using AuroraJudge.Shared.Models;
using Microsoft.AspNetCore.Http;

namespace AuroraJudge.Application.Services;

/// <summary>
/// 认证服务接口
/// </summary>
public interface IAuthService
{
    Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<LoginResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<UserDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// 题目服务接口
/// </summary>
public interface IProblemService
{
    Task<PagedResponse<ProblemListDto>> GetProblemsAsync(int page, int pageSize, string? search, Guid? tagId, int? difficulty, Guid? userId, CancellationToken cancellationToken = default);
    Task<ProblemDto> GetProblemAsync(Guid id, Guid? userId, Guid? contestId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TagDto>> GetTagsAsync(CancellationToken cancellationToken = default);
    Task<TagDto> CreateTagAsync(CreateTagRequest request, CancellationToken cancellationToken = default);
    Task<TagDto> UpdateTagAsync(Guid tagId, CreateTagRequest request, CancellationToken cancellationToken = default);
    Task DeleteTagAsync(Guid tagId, CancellationToken cancellationToken = default);
    Task<ProblemDto> CreateProblemAsync(CreateProblemRequest request, Guid creatorId, CancellationToken cancellationToken = default);
    Task<ProblemDto> UpdateProblemAsync(Guid id, UpdateProblemRequest request, CancellationToken cancellationToken = default);
    Task DeleteProblemAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TestCaseDto>> GetTestCasesAsync(Guid problemId, CancellationToken cancellationToken = default);
    Task<TestCaseDto> AddTestCaseAsync(Guid problemId, CreateTestCaseRequest request, IFormFile inputFile, IFormFile outputFile, CancellationToken cancellationToken = default);
    Task DeleteTestCaseAsync(Guid problemId, Guid testCaseId, CancellationToken cancellationToken = default);
    Task<(Stream Stream, string FileName)> DownloadTestCaseFileAsync(Guid problemId, Guid testCaseId, bool isInput, CancellationToken cancellationToken = default);
    Task RejudgeProblemAsync(Guid problemId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 排行榜服务接口
/// </summary>
public interface IRankingService
{
    Task<PagedResponse<RankingUserDto>> GetRankingsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}

/// <summary>
/// 提交服务接口
/// </summary>
public interface ISubmissionService
{
    Task<PagedResponse<SubmissionDto>> GetSubmissionsAsync(SubmissionQueryRequest query, CancellationToken cancellationToken = default);
    Task<SubmissionDetailDto> GetSubmissionAsync(Guid id, Guid? requesterId, bool forceViewCode = false, CancellationToken cancellationToken = default);
    Task<SubmissionDto> CreateSubmissionAsync(CreateSubmissionRequest request, Guid userId, string? ipAddress, CancellationToken cancellationToken = default);
    Task RejudgeSubmissionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubmissionSimilarityDto>> GetSimilarSubmissionsAsync(
        Guid submissionId,
        int top = 10,
        int candidateLimit = 500,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 比赛服务接口
/// </summary>
public interface IContestService
{
    Task<PagedResponse<ContestDto>> GetContestsAsync(int page, int pageSize, int? status, int? type, CancellationToken cancellationToken = default);
    Task<ContestDetailDto> GetContestAsync(Guid id, Guid? userId, CancellationToken cancellationToken = default);
    Task<ContestDto> CreateContestAsync(CreateContestRequest request, Guid creatorId, CancellationToken cancellationToken = default);
    Task<ContestDto> UpdateContestAsync(Guid id, UpdateContestRequest request, CancellationToken cancellationToken = default);
    Task DeleteContestAsync(Guid id, CancellationToken cancellationToken = default);
    Task RegisterContestAsync(Guid contestId, Guid userId, string? password, CancellationToken cancellationToken = default);
    Task UnregisterContestAsync(Guid contestId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContestRankingDto>> GetStandingsAsync(Guid contestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContestProblemDto>> GetContestProblemsAsync(Guid contestId, Guid? userId, CancellationToken cancellationToken = default);
    Task UpdateContestProblemsAsync(Guid contestId, IReadOnlyList<ContestProblemRequest> problems, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContestAnnouncementDto>> GetContestAnnouncementsAsync(Guid contestId, CancellationToken cancellationToken = default);
    Task<ContestAnnouncementDto> CreateAnnouncementAsync(Guid contestId, CreateContestAnnouncementRequest request, Guid publisherId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 管理服务接口
/// </summary>
public interface IAdminService
{
    // 用户管理
    Task<PagedResponse<UserDto>> GetUsersAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default);
    Task BanUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UnbanUserAsync(Guid userId, CancellationToken cancellationToken = default);
    
    // 角色权限管理
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
    Task<RoleDto> UpdateRoleAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default);
    Task DeleteRoleAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default);
    Task AssignRoleAsync(Guid userId, Guid roleId, Guid operatorId, CancellationToken cancellationToken = default);
    Task RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    
    // 系统配置
    Task<IReadOnlyList<SystemConfigDto>> GetSystemConfigsAsync(CancellationToken cancellationToken = default);
    Task UpdateSystemConfigAsync(string key, string value, Guid operatorId, CancellationToken cancellationToken = default);
    
    // 语言配置
    Task<IReadOnlyList<LanguageConfigDto>> GetLanguageConfigsAsync(CancellationToken cancellationToken = default);
    Task<LanguageConfigDto> CreateLanguageConfigAsync(CreateLanguageConfigRequest request, CancellationToken cancellationToken = default);
    
    // 判题机管理
    Task<IReadOnlyList<JudgerStatusDto>> GetJudgerStatusesAsync(CancellationToken cancellationToken = default);
    Task SetJudgerEnabledAsync(Guid judgerId, bool enabled, CancellationToken cancellationToken = default);
    
    // 审计日志
    Task<PagedResponse<AuditLogDto>> GetAuditLogsAsync(AuditLogQueryRequest query, CancellationToken cancellationToken = default);
}

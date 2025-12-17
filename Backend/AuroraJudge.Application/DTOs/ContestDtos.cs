using AuroraJudge.Domain.Enums;

namespace AuroraJudge.Application.DTOs;

public record ContestDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartTime,
    DateTime EndTime,
    DateTime? FreezeTime,
    ContestType Type,
    ContestVisibility Visibility,
    bool IsRated,
    ContestStatus Status,
    int ParticipantCount,
    int ProblemCount,
    Guid CreatorId,
    string CreatorName,
    DateTime CreatedAt
);

public record ContestDetailDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartTime,
    DateTime EndTime,
    DateTime? FreezeTime,
    ContestType Type,
    ContestVisibility Visibility,
    bool IsRated,
    int? RatingFloor,
    int? RatingCeiling,
    bool AllowLateSubmission,
    double LateSubmissionPenalty,
    bool ShowRanking,
    bool AllowViewOthersCode,
    bool PublishProblemsAfterEnd,
    int? MaxParticipants,
    string? Rules,
    ContestStatus Status,
    int ParticipantCount,
    IReadOnlyList<ContestProblemDto> Problems,
    bool IsRegistered,
    Guid CreatorId,
    string CreatorName,
    DateTime CreatedAt
);

public record ContestProblemDto(
    Guid ProblemId,
    int Order,
    string Label,
    string Title,
    int? Score,
    string? Color,
    int SubmissionCount,
    int AcceptedCount,
    bool? Solved // null 表示未参赛
);

public record CreateContestRequest(
    string Title,
    string? Description,
    DateTime StartTime,
    DateTime EndTime,
    DateTime? FreezeTime,
    ContestType Type,
    ContestVisibility Visibility,
    string? Password,
    bool IsRated,
    int? RatingFloor,
    int? RatingCeiling,
    bool AllowLateSubmission,
    double LateSubmissionPenalty,
    bool ShowRanking,
    bool AllowViewOthersCode,
    bool PublishProblemsAfterEnd,
    int? MaxParticipants,
    string? Rules,
    IReadOnlyList<ContestProblemRequest>? Problems
);

public record ContestProblemRequest(
    Guid ProblemId,
    string Label,
    int Order,
    int? Score,
    string? Color
);

public record UpdateContestRequest(
    string Title,
    string? Description,
    DateTime StartTime,
    DateTime EndTime,
    DateTime? FreezeTime,
    ContestType Type,
    ContestVisibility Visibility,
    string? Password,
    bool IsRated,
    int? RatingFloor,
    int? RatingCeiling,
    bool AllowLateSubmission,
    double LateSubmissionPenalty,
    bool ShowRanking,
    bool AllowViewOthersCode,
    bool PublishProblemsAfterEnd,
    int? MaxParticipants,
    string? Rules
);

public record ContestRegisterRequest(
    string? Password
);

public record ContestRankingDto(
    int Rank,
    Guid UserId,
    string Username,
    string? Avatar,
    int Score,
    int Penalty,
    int SolvedCount,
    IReadOnlyList<ContestProblemResultDto> Problems
);

public record ContestProblemResultDto(
    string Label,
    bool Solved,
    bool FirstBlood,
    int Attempts,
    int? SolvedTime, // 距离比赛开始的分钟数
    int? Score // OI 模式分数
);

public record ContestAnnouncementDto(
    Guid Id,
    string Title,
    string Content,
    bool IsPinned,
    Guid? ProblemId,
    string? ProblemLabel,
    DateTime CreatedAt
);

public record CreateContestAnnouncementRequest(
    string Title,
    string Content,
    bool IsPinned,
    Guid? ProblemId
);

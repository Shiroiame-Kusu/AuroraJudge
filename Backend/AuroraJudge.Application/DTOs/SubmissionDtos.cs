using AuroraJudge.Domain.Enums;

namespace AuroraJudge.Application.DTOs;

public record SubmissionDto(
    Guid Id,
    Guid ProblemId,
    string ProblemTitle,
    Guid UserId,
    string Username,
    Guid? ContestId,
    string? ContestTitle,
    string Language,
    int CodeLength,
    JudgeStatus Status,
    int? Score,
    int? TimeUsed,
    int? MemoryUsed,
    DateTime SubmittedAt,
    DateTime? JudgedAt
);

public record SubmissionDetailDto(
    Guid Id,
    Guid ProblemId,
    string ProblemTitle,
    Guid UserId,
    string Username,
    Guid? ContestId,
    string? ContestTitle,
    string Code,
    string Language,
    int CodeLength,
    JudgeStatus Status,
    int? Score,
    int? TimeUsed,
    int? MemoryUsed,
    string? CompileMessage,
    string? JudgeMessage,
    DateTime SubmittedAt,
    DateTime? JudgedAt,
    IReadOnlyList<JudgeResultDto> Results
);

public record SubmissionSimilarityDto(
    Guid SubmissionId,
    Guid UserId,
    string Username,
    Guid ProblemId,
    string ProblemTitle,
    string Language,
    int CodeLength,
    DateTime SubmittedAt,
    double Similarity
);

public record JudgeResultDto(
    int TestCaseOrder,
    int? Subtask,
    JudgeStatus Status,
    int TimeUsed,
    int MemoryUsed,
    int Score,
    string? Message
);

public record CreateSubmissionRequest(
    Guid ProblemId,
    string Code,
    string Language,
    Guid? ContestId
);

public record SubmissionQueryRequest(
    int Page = 1,
    int PageSize = 20,
    Guid? UserId = null,
    Guid? ProblemId = null,
    Guid? ContestId = null,
    JudgeStatus? Status = null,
    string? Language = null,
    string? Username = null
);

using AuroraJudge.Domain.Enums;

namespace AuroraJudge.Application.DTOs;

public record ProblemDto(
    Guid Id,
    string Title,
    string Description,
    string InputFormat,
    string OutputFormat,
    string? SampleInput,
    string? SampleOutput,
    string? Hint,
    string? Source,
    int TimeLimit,
    int MemoryLimit,
    int StackLimit,
    int OutputLimit,
    JudgeMode JudgeMode,
    string? SpecialJudgeCode,
    string? SpecialJudgeLanguage,
    string? AllowedLanguages,
    ProblemVisibility Visibility,
    ProblemDifficulty Difficulty,
    int SubmissionCount,
    int AcceptedCount,
    double AcceptRate,
    IReadOnlyList<TagDto> Tags,
    DateTime CreatedAt
);

public record ProblemListDto(
    Guid Id,
    string Title,
    ProblemDifficulty Difficulty,
    int SubmissionCount,
    int AcceptedCount,
    double AcceptRate,
    IReadOnlyList<TagDto> Tags,
    bool? Solved // null 表示未登录
);

public record CreateProblemRequest(
    string Title,
    string Description,
    string InputFormat,
    string OutputFormat,
    string? SampleInput,
    string? SampleOutput,
    string? Hint,
    string? Source,
    int TimeLimit,
    int MemoryLimit,
    int StackLimit,
    int OutputLimit,
    JudgeMode JudgeMode,
    string? SpecialJudgeCode,
    string? SpecialJudgeLanguage,
    string? AllowedLanguages,
    ProblemVisibility Visibility,
    ProblemDifficulty Difficulty,
    IReadOnlyList<Guid>? TagIds
);

public record UpdateProblemRequest(
    string Title,
    string Description,
    string InputFormat,
    string OutputFormat,
    string? SampleInput,
    string? SampleOutput,
    string? Hint,
    string? Source,
    int TimeLimit,
    int MemoryLimit,
    int StackLimit,
    int OutputLimit,
    JudgeMode JudgeMode,
    string? SpecialJudgeCode,
    string? SpecialJudgeLanguage,
    string? AllowedLanguages,
    ProblemVisibility Visibility,
    ProblemDifficulty Difficulty,
    IReadOnlyList<Guid>? TagIds
);

public record TestCaseDto(
    Guid Id,
    int Order,
    long InputSize,
    long OutputSize,
    int Score,
    bool IsSample,
    int? Subtask,
    string? Description
);

public record CreateTestCaseRequest(
    int Order,
    int Score,
    bool IsSample,
    int? Subtask,
    string? Description
    // 文件通过 multipart/form-data 上传
);

public record TagDto(
    Guid Id,
    string Name,
    string? Color,
    string? Category,
    int UsageCount
);

public record CreateTagRequest(
    string Name,
    string? Color,
    string? Category
);

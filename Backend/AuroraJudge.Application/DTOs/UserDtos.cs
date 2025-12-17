using AuroraJudge.Domain.Enums;

namespace AuroraJudge.Application.DTOs;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string? Avatar,
    string? Bio,
    string? RealName,
    string? Organization,
    UserStatus Status,
    int SolvedCount,
    int SubmissionCount,
    int Rating,
    int MaxRating,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions
);

public record UserProfileDto(
    Guid Id,
    string Username,
    string? Avatar,
    string? Bio,
    string? Organization,
    int SolvedCount,
    int SubmissionCount,
    int Rating,
    int MaxRating,
    DateTime CreatedAt,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> SolvedProblemIds
);

public record UpdateProfileRequest(
    string? Avatar,
    string? Bio,
    string? RealName,
    string? Organization
);

public record UserListDto(
    Guid Id,
    string Username,
    string? Avatar,
    int SolvedCount,
    int Rating,
    int Rank
);

public record RankingUserDto(
    int Rank,
    Guid UserId,
    string Username,
    string? Nickname,
    int AcceptedCount,
    int SubmissionCount,
    int Score
);

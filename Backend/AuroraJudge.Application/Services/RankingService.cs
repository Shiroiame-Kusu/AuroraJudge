using AuroraJudge.Application.DTOs;
using AuroraJudge.Domain.Interfaces;
using AuroraJudge.Shared.Models;

namespace AuroraJudge.Application.Services;

public class RankingService : IRankingService
{
    private readonly IUserRepository _userRepository;

    public RankingService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PagedResponse<RankingUserDto>> GetRankingsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (users, totalCount) = await _userRepository.GetLeaderboardPagedAsync(page, pageSize, cancellationToken);

        var items = users
            .Select((u, index) => new RankingUserDto(
                Rank: (page - 1) * pageSize + index + 1,
                UserId: u.Id,
                Username: u.Username,
                Nickname: u.DisplayName,
                AcceptedCount: u.SolvedCount,
                SubmissionCount: u.SubmissionCount,
                Score: u.Rating
            ))
            .ToList();

        return new PagedResponse<RankingUserDto>
        {
            Items = items,
            Total = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}

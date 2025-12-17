using AuroraJudge.Application.DTOs;
using AuroraJudge.Domain.Common;
using AuroraJudge.Domain.Entities;
using AuroraJudge.Domain.Enums;
using AuroraJudge.Domain.Interfaces;
using AuroraJudge.Shared.Models;

namespace AuroraJudge.Application.Services;

public class ContestService : IContestService
{
    private readonly IContestRepository _contestRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public ContestService(
        IContestRepository contestRepository,
        IUnitOfWork unitOfWork)
    {
        _contestRepository = contestRepository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<PagedResponse<ContestDto>> GetContestsAsync(
        int page, 
        int pageSize, 
        int? status = null,
        int? type = null,
        CancellationToken cancellationToken = default)
    {
        ContestStatus? statusEnum = status.HasValue ? (ContestStatus)status.Value : null;
        ContestType? typeEnum = type.HasValue ? (ContestType)type.Value : null;
        
        var (items, totalCount) = await _contestRepository.GetPagedAsync(
            page, pageSize, statusEnum, typeEnum, cancellationToken);
        
        var dtos = items.Select(MapToContestDto).ToList();
        
        return new PagedResponse<ContestDto>
        {
            Items = dtos,
            Total = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
    
    public async Task<ContestDetailDto> GetContestAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var contest = await _contestRepository.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("比赛不存在");
        
        var isRegistered = userId.HasValue && 
            await _contestRepository.IsUserRegisteredAsync(id, userId.Value, cancellationToken);
        
        return MapToContestDetailDto(contest, isRegistered);
    }
    
    public async Task<ContestDto> CreateContestAsync(CreateContestRequest request, Guid creatorId, CancellationToken cancellationToken = default)
    {
        var contest = new Contest
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            FreezeTime = request.FreezeTime,
            Type = request.Type,
            Visibility = request.Visibility,
            Password = request.Password,
            IsRated = request.IsRated,
            RatingFloor = request.RatingFloor,
            RatingCeiling = request.RatingCeiling,
            AllowLateSubmission = request.AllowLateSubmission,
            LateSubmissionPenalty = request.LateSubmissionPenalty,
            ShowRanking = request.ShowRanking,
            AllowViewOthersCode = request.AllowViewOthersCode,
            PublishProblemsAfterEnd = request.PublishProblemsAfterEnd,
            MaxParticipants = request.MaxParticipants,
            Rules = request.Rules,
            CreatorId = creatorId,
            CreatedAt = DateTime.UtcNow
        };
        
        await _contestRepository.AddAsync(contest, cancellationToken);
        
        // Add contest problems
        if (request.Problems?.Any() == true)
        {
            foreach (var cp in request.Problems)
            {
                var contestProblem = new ContestProblem
                {
                    ContestId = contest.Id,
                    ProblemId = cp.ProblemId,
                    Label = cp.Label,
                    Order = cp.Order,
                    Score = cp.Score,
                    Color = cp.Color
                };
                await _contestRepository.AddProblemAsync(contestProblem, cancellationToken);
            }
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return MapToContestDto(contest);
    }
    
    public async Task<ContestDto> UpdateContestAsync(Guid id, UpdateContestRequest request, CancellationToken cancellationToken = default)
    {
        var contest = await _contestRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("比赛不存在");
        
        contest.Title = request.Title;
        contest.Description = request.Description;
        contest.StartTime = request.StartTime;
        contest.EndTime = request.EndTime;
        contest.FreezeTime = request.FreezeTime;
        contest.Type = request.Type;
        contest.Visibility = request.Visibility;
        contest.Password = request.Password;
        contest.IsRated = request.IsRated;
        contest.RatingFloor = request.RatingFloor;
        contest.RatingCeiling = request.RatingCeiling;
        contest.AllowLateSubmission = request.AllowLateSubmission;
        contest.LateSubmissionPenalty = request.LateSubmissionPenalty;
        contest.ShowRanking = request.ShowRanking;
        contest.AllowViewOthersCode = request.AllowViewOthersCode;
        contest.PublishProblemsAfterEnd = request.PublishProblemsAfterEnd;
        contest.MaxParticipants = request.MaxParticipants;
        contest.Rules = request.Rules;
        contest.UpdatedAt = DateTime.UtcNow;
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return MapToContestDto(contest);
    }
    
    public async Task DeleteContestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contest = await _contestRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("比赛不存在");
        
        contest.IsDeleted = true;
        contest.DeletedAt = DateTime.UtcNow;
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    
    public async Task RegisterContestAsync(Guid contestId, Guid userId, string? password, CancellationToken cancellationToken = default)
    {
        var contest = await _contestRepository.GetByIdAsync(contestId, cancellationToken)
            ?? throw new ValidationException("比赛不存在");
        
        // 检查比赛是否已结束
        if (DateTime.UtcNow > contest.EndTime)
        {
            throw new ValidationException("比赛已结束");
        }
        
        // 检查密码
        if (contest.Visibility == ContestVisibility.Protected && contest.Password != password)
        {
            throw new ValidationException("密码错误");
        }
        
        // 检查人数限制
        var currentCount = contest.Participants?.Count ?? 0;
        if (contest.MaxParticipants.HasValue && currentCount >= contest.MaxParticipants.Value)
        {
            throw new ValidationException("参赛人数已满");
        }
        
        // 检查是否已报名
        if (await _contestRepository.IsUserRegisteredAsync(contestId, userId, cancellationToken))
        {
            throw new ValidationException("已经报名该比赛");
        }
        
        var participant = new ContestParticipant
        {
            ContestId = contestId,
            UserId = userId,
            RegisteredAt = DateTime.UtcNow,
            Status = 0 // Registered
        };
        
        await _contestRepository.AddParticipantAsync(participant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    
    public async Task UnregisterContestAsync(Guid contestId, Guid userId, CancellationToken cancellationToken = default)
    {
        var contest = await _contestRepository.GetByIdAsync(contestId, cancellationToken)
            ?? throw new ValidationException("比赛不存在");
        
        // 检查比赛是否已开始
        if (DateTime.UtcNow >= contest.StartTime)
        {
            throw new ValidationException("比赛已开始，无法取消报名");
        }
        
        if (!await _contestRepository.IsUserRegisteredAsync(contestId, userId, cancellationToken))
        {
            throw new ValidationException("未报名该比赛");
        }
        
        await _contestRepository.RemoveParticipantAsync(contestId, userId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<ContestRankingDto>> GetStandingsAsync(Guid contestId, CancellationToken cancellationToken = default)
    {
        var contest = await _contestRepository.GetByIdAsync(contestId, cancellationToken)
            ?? throw new KeyNotFoundException("比赛不存在");
        
        var now = DateTime.UtcNow;
        if (!contest.ShowRanking && now < contest.EndTime)
        {
            return [];
        }
        
        var participants = await _contestRepository.GetStandingsAsync(contestId, cancellationToken);
        
        var rankings = new List<ContestRankingDto>();
        var rank = 1;
        
        foreach (var participant in participants.OrderByDescending(p => p.Score).ThenBy(p => p.Penalty))
        {
            rankings.Add(new ContestRankingDto(
                Rank: rank++,
                UserId: participant.UserId,
                Username: participant.User?.Username ?? "",
                Avatar: participant.User?.Avatar,
                Score: participant.Score,
                Penalty: participant.Penalty,
                SolvedCount: 0, // Would need to calculate from submissions
                Problems: [] // Would need to build from contest submissions
            ));
        }
        
        return rankings;
    }
    
    public async Task<IReadOnlyList<ContestProblemDto>> GetContestProblemsAsync(Guid contestId, Guid? userId, CancellationToken cancellationToken = default)
    {
        var contest = await _contestRepository.GetByIdWithDetailsAsync(contestId, cancellationToken)
            ?? throw new KeyNotFoundException("比赛不存在");
        
        return contest.ContestProblems?
            .OrderBy(cp => cp.Order)
            .Select(cp => new ContestProblemDto(
                ProblemId: cp.ProblemId,
                Order: cp.Order,
                Label: cp.Label,
                Title: cp.Problem?.Title ?? "",
                Score: cp.Score,
                Color: cp.Color,
                SubmissionCount: cp.SubmissionCount,
                AcceptedCount: cp.AcceptedCount,
                Solved: null // Would need to check user submissions
            ))
            .ToList() ?? [];
    }
    
    public async Task UpdateContestProblemsAsync(Guid contestId, IReadOnlyList<ContestProblemRequest> problems, CancellationToken cancellationToken = default)
    {
        var contest = await _contestRepository.GetByIdAsync(contestId, cancellationToken)
            ?? throw new KeyNotFoundException("比赛不存在");
        
        // Clear existing problems
        await _contestRepository.ClearProblemsAsync(contestId, cancellationToken);
        
        // Add new problems
        foreach (var cp in problems)
        {
            var contestProblem = new ContestProblem
            {
                ContestId = contestId,
                ProblemId = cp.ProblemId,
                Label = cp.Label,
                Order = cp.Order,
                Score = cp.Score,
                Color = cp.Color
            };
            await _contestRepository.AddProblemAsync(contestProblem, cancellationToken);
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<ContestAnnouncementDto>> GetContestAnnouncementsAsync(Guid contestId, CancellationToken cancellationToken = default)
    {
        var announcements = await _contestRepository.GetAnnouncementsAsync(contestId, cancellationToken);
        var contest = await _contestRepository.GetByIdWithDetailsAsync(contestId, cancellationToken);
        
        return announcements
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new ContestAnnouncementDto(
                Id: a.Id,
                Title: a.Title,
                Content: a.Content,
                IsPinned: a.IsPinned,
                ProblemId: null, // Announcement entity doesn't have ProblemId
                ProblemLabel: null,
                CreatedAt: a.CreatedAt
            ))
            .ToList();
    }
    
    public async Task<ContestAnnouncementDto> CreateAnnouncementAsync(Guid contestId, CreateContestAnnouncementRequest request, Guid publisherId, CancellationToken cancellationToken = default)
    {
        var contest = await _contestRepository.GetByIdWithDetailsAsync(contestId, cancellationToken)
            ?? throw new KeyNotFoundException("比赛不存在");
        
        var announcement = new Announcement
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            IsPinned = request.IsPinned,
            CreatedAt = DateTime.UtcNow
        };
        
        // Note: The Announcement entity may need modification to support contest announcements
        // For now, we use ContestAnnouncement which is a different entity
        var contestAnnouncement = new ContestAnnouncement
        {
            Id = Guid.NewGuid(),
            ContestId = contestId,
            Title = request.Title,
            Content = request.Content,
            IsPinned = request.IsPinned,
            ProblemId = request.ProblemId,
            CreatedAt = DateTime.UtcNow
        };
        
        contest.Announcements.Add(contestAnnouncement);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return new ContestAnnouncementDto(
            Id: contestAnnouncement.Id,
            Title: contestAnnouncement.Title,
            Content: contestAnnouncement.Content,
            IsPinned: contestAnnouncement.IsPinned,
            ProblemId: contestAnnouncement.ProblemId,
            ProblemLabel: contest.ContestProblems?.FirstOrDefault(cp => cp.ProblemId == contestAnnouncement.ProblemId)?.Label,
            CreatedAt: contestAnnouncement.CreatedAt
        );
    }
    
    #region Mapping Helpers
    
    private static ContestStatus GetContestStatus(Contest contest)
    {
        var now = DateTime.UtcNow;
        if (now < contest.StartTime) return ContestStatus.Pending;
        if (now > contest.EndTime) return ContestStatus.Ended;
        if (contest.FreezeTime.HasValue && now >= contest.FreezeTime.Value) return ContestStatus.Frozen;
        return ContestStatus.Running;
    }
    
    private static ContestDto MapToContestDto(Contest contest)
    {
        return new ContestDto(
            Id: contest.Id,
            Title: contest.Title,
            Description: contest.Description,
            StartTime: contest.StartTime,
            EndTime: contest.EndTime,
            FreezeTime: contest.FreezeTime,
            Type: contest.Type,
            Visibility: contest.Visibility,
            IsRated: contest.IsRated,
            Status: GetContestStatus(contest),
            ParticipantCount: contest.Participants?.Count ?? 0,
            ProblemCount: contest.ContestProblems?.Count ?? 0,
            CreatorId: contest.CreatorId,
            CreatorName: contest.Creator?.Username ?? "",
            CreatedAt: contest.CreatedAt
        );
    }
    
    private static ContestDetailDto MapToContestDetailDto(Contest contest, bool isRegistered)
    {
        var problems = contest.ContestProblems?
            .OrderBy(cp => cp.Order)
            .Select(cp => new ContestProblemDto(
                ProblemId: cp.ProblemId,
                Order: cp.Order,
                Label: cp.Label,
                Title: cp.Problem?.Title ?? "",
                Score: cp.Score,
                Color: cp.Color,
                SubmissionCount: cp.SubmissionCount,
                AcceptedCount: cp.AcceptedCount,
                Solved: null
            ))
            .ToList() ?? [];
        
        return new ContestDetailDto(
            Id: contest.Id,
            Title: contest.Title,
            Description: contest.Description,
            StartTime: contest.StartTime,
            EndTime: contest.EndTime,
            FreezeTime: contest.FreezeTime,
            Type: contest.Type,
            Visibility: contest.Visibility,
            IsRated: contest.IsRated,
            RatingFloor: contest.RatingFloor,
            RatingCeiling: contest.RatingCeiling,
            AllowLateSubmission: contest.AllowLateSubmission,
            LateSubmissionPenalty: contest.LateSubmissionPenalty,
            ShowRanking: contest.ShowRanking,
            AllowViewOthersCode: contest.AllowViewOthersCode,
            PublishProblemsAfterEnd: contest.PublishProblemsAfterEnd,
            MaxParticipants: contest.MaxParticipants,
            Rules: contest.Rules,
            Status: GetContestStatus(contest),
            ParticipantCount: contest.Participants?.Count ?? 0,
            Problems: problems,
            IsRegistered: isRegistered,
            CreatorId: contest.CreatorId,
            CreatorName: contest.Creator?.Username ?? "",
            CreatedAt: contest.CreatedAt
        );
    }
    
    #endregion
}

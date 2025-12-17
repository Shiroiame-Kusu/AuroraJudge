using AuroraJudge.Domain.Common;
using AuroraJudge.Domain.Entities;
using AuroraJudge.Domain.Enums;
using AuroraJudge.Domain.Interfaces;
using AuroraJudge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuroraJudge.Infrastructure.Repositories;

public class ContestRepository : IContestRepository
{
    private readonly ApplicationDbContext _context;
    
    public ContestRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<Contest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Contests.FindAsync([id], cancellationToken);
    }
    
    public async Task<Contest?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Contests
            .Include(c => c.ContestProblems)
                .ThenInclude(cp => cp.Problem)
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
    
    public async Task<(IReadOnlyList<Contest> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, ContestStatus? status, ContestType? type, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Contests
            .Include(c => c.ContestProblems)
            .Include(c => c.Participants)
            .Where(c => c.Visibility == ContestVisibility.Public)
            .AsQueryable();
        
        if (status.HasValue)
        {
            var now = DateTime.UtcNow;
            query = status.Value switch
            {
                ContestStatus.Pending => query.Where(c => c.StartTime > now),
                ContestStatus.Running => query.Where(c => c.StartTime <= now && c.EndTime > now),
                ContestStatus.Ended => query.Where(c => c.EndTime <= now),
                _ => query
            };
        }
        
        if (type.HasValue)
        {
            query = query.Where(c => c.Type == type.Value);
        }
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(c => c.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        
        return (items, totalCount);
    }
    
    public async Task AddAsync(Contest contest, CancellationToken cancellationToken = default)
    {
        await _context.Contests.AddAsync(contest, cancellationToken);
    }
    
    public async Task<bool> IsUserRegisteredAsync(Guid contestId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.ContestParticipants
            .AnyAsync(cp => cp.ContestId == contestId && cp.UserId == userId, cancellationToken);
    }
    
    public async Task AddParticipantAsync(ContestParticipant participant, CancellationToken cancellationToken = default)
    {
        await _context.ContestParticipants.AddAsync(participant, cancellationToken);
    }
    
    public async Task RemoveParticipantAsync(Guid contestId, Guid userId, CancellationToken cancellationToken = default)
    {
        var participant = await _context.ContestParticipants
            .FirstOrDefaultAsync(cp => cp.ContestId == contestId && cp.UserId == userId, cancellationToken);
        
        if (participant != null)
        {
            _context.ContestParticipants.Remove(participant);
        }
    }
    
    public async Task<IReadOnlyList<ContestParticipant>> GetStandingsAsync(Guid contestId, CancellationToken cancellationToken = default)
    {
        return await _context.ContestParticipants
            .Include(cp => cp.User)
            .Where(cp => cp.ContestId == contestId)
            .OrderByDescending(cp => cp.Score)
            .ThenBy(cp => cp.Penalty)
            .ToListAsync(cancellationToken);
    }
    
    public async Task ClearProblemsAsync(Guid contestId, CancellationToken cancellationToken = default)
    {
        var problems = await _context.ContestProblems
            .Where(cp => cp.ContestId == contestId)
            .ToListAsync(cancellationToken);
        
        _context.ContestProblems.RemoveRange(problems);
    }
    
    public async Task AddProblemAsync(ContestProblem problem, CancellationToken cancellationToken = default)
    {
        await _context.ContestProblems.AddAsync(problem, cancellationToken);
    }
    
    public async Task<IReadOnlyList<Announcement>> GetAnnouncementsAsync(Guid contestId, CancellationToken cancellationToken = default)
    {
        // Note: This returns global announcements. For contest-specific announcements,
        // use ContestAnnouncements table. This implementation returns all published announcements
        // as a fallback since the interface expects Announcement type.
        return await _context.Announcements
            .Where(a => a.Status == AnnouncementStatus.Published)
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task AddAnnouncementAsync(Announcement announcement, CancellationToken cancellationToken = default)
    {
        await _context.Announcements.AddAsync(announcement, cancellationToken);
    }
}

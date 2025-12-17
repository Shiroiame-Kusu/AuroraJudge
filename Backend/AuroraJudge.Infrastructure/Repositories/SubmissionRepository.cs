using AuroraJudge.Domain.Common;
using AuroraJudge.Domain.Entities;
using AuroraJudge.Domain.Enums;
using AuroraJudge.Domain.Interfaces;
using AuroraJudge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuroraJudge.Infrastructure.Repositories;

public class SubmissionRepository : ISubmissionRepository
{
    private readonly ApplicationDbContext _context;
    
    public SubmissionRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<Submission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Submissions.FindAsync([id], cancellationToken);
    }
    
    public async Task<Submission?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Submissions
            .Include(s => s.Problem)
            .Include(s => s.User)
            .Include(s => s.JudgeResults)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }
    
    public async Task<(IReadOnlyList<Submission> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, Guid? userId, Guid? problemId, Guid? contestId, 
        string? language, JudgeStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _context.Submissions
            .Include(s => s.Problem)
            .Include(s => s.User)
            .AsQueryable();
        
        if (userId.HasValue)
        {
            query = query.Where(s => s.UserId == userId.Value);
        }
        
        if (problemId.HasValue)
        {
            query = query.Where(s => s.ProblemId == problemId.Value);
        }
        
        if (contestId.HasValue)
        {
            query = query.Where(s => s.ContestId == contestId.Value);
        }
        
        if (!string.IsNullOrEmpty(language))
        {
            query = query.Where(s => s.Language == language);
        }
        
        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        
        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Submission>> GetRecentForSimilarityAsync(
        Guid problemId,
        string language,
        Guid excludeSubmissionId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await _context.Submissions
            .Include(s => s.Problem)
            .Include(s => s.User)
            .Where(s => s.ProblemId == problemId && s.Language == language && s.Id != excludeSubmissionId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
    
    public async Task AddAsync(Submission submission, CancellationToken cancellationToken = default)
    {
        await _context.Submissions.AddAsync(submission, cancellationToken);
    }
}

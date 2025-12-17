using AuroraJudge.Domain.Common;
using AuroraJudge.Domain.Entities;
using AuroraJudge.Domain.Enums;
using AuroraJudge.Domain.Interfaces;
using AuroraJudge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuroraJudge.Infrastructure.Repositories;

public class ProblemRepository : IProblemRepository
{
    private readonly ApplicationDbContext _context;
    
    public ProblemRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<Problem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Problems.FindAsync([id], cancellationToken);
    }
    
    public async Task<Problem?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Problems
            .Include(p => p.ProblemTags)
                .ThenInclude(pt => pt.Tag)
            .Include(p => p.Creator)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
    
    public async Task<(IReadOnlyList<Problem> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, Guid? tagId, ProblemDifficulty? difficulty, 
        ProblemVisibility? visibility, CancellationToken cancellationToken = default)
    {
        var query = _context.Problems
            .Include(p => p.ProblemTags)
                .ThenInclude(pt => pt.Tag)
            .AsQueryable();
        
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p => p.Title.Contains(search));
        }
        
        if (tagId.HasValue)
        {
            query = query.Where(p => p.ProblemTags.Any(pt => pt.TagId == tagId.Value));
        }
        
        if (difficulty.HasValue)
        {
            query = query.Where(p => p.Difficulty == difficulty.Value);
        }
        
        if (visibility.HasValue)
        {
            query = query.Where(p => p.Visibility == visibility.Value);
        }
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        
        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Problem> Items, int TotalCount)> GetPagedForViewerAsync(
        int page,
        int pageSize,
        string? search,
        Guid? tagId,
        ProblemDifficulty? difficulty,
        Guid? viewerId,
        bool canViewHidden,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Problems
            .Include(p => p.ProblemTags)
                .ThenInclude(pt => pt.Tag)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p => p.Title.Contains(search));
        }

        if (tagId.HasValue)
        {
            query = query.Where(p => p.ProblemTags.Any(pt => pt.TagId == tagId.Value));
        }

        if (difficulty.HasValue)
        {
            query = query.Where(p => p.Difficulty == difficulty.Value);
        }

        // Visibility filtering
        if (!canViewHidden)
        {
            if (viewerId.HasValue)
            {
                var uid = viewerId.Value;
                query = query.Where(p => p.Visibility == ProblemVisibility.Public || p.CreatorId == uid);
            }
            else
            {
                query = query.Where(p => p.Visibility == ProblemVisibility.Public);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlySet<Guid>> GetSolvedProblemIdsAsync(
        Guid userId,
        IReadOnlyList<Guid> problemIds,
        CancellationToken cancellationToken = default)
    {
        if (problemIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var solvedIds = await _context.UserSolvedProblems
            .AsNoTracking()
            .Where(usp => usp.UserId == userId && problemIds.Contains(usp.ProblemId))
            .Select(usp => usp.ProblemId)
            .ToListAsync(cancellationToken);

        return new HashSet<Guid>(solvedIds);
    }

    public async Task<IReadOnlyList<Tag>> GetAllTagsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tags
            .AsNoTracking()
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tag?> GetTagByIdAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        return await _context.Tags
            .FirstOrDefaultAsync(t => t.Id == tagId, cancellationToken);
    }

    public async Task<Tag?> GetTagByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Tags
            .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
    }

    public async Task AddTagAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        await _context.Tags.AddAsync(tag, cancellationToken);
    }

    public async Task DeleteTagAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        var tag = await _context.Tags
            .Include(t => t.ProblemTags)
            .FirstOrDefaultAsync(t => t.Id == tagId, cancellationToken);

        if (tag != null)
        {
            _context.Tags.Remove(tag);
        }
    }
    
    public async Task AddAsync(Problem problem, CancellationToken cancellationToken = default)
    {
        await _context.Problems.AddAsync(problem, cancellationToken);
    }
    
    public async Task<IReadOnlyList<TestCase>> GetTestCasesAsync(Guid problemId, CancellationToken cancellationToken = default)
    {
        return await _context.TestCases
            .Where(tc => tc.ProblemId == problemId)
            .OrderBy(tc => tc.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task<TestCase?> GetTestCaseByIdAsync(Guid problemId, Guid testCaseId, CancellationToken cancellationToken = default)
    {
        return await _context.TestCases
            .FirstOrDefaultAsync(tc => tc.ProblemId == problemId && tc.Id == testCaseId, cancellationToken);
    }
    
    public async Task AddTestCaseAsync(TestCase testCase, CancellationToken cancellationToken = default)
    {
        await _context.TestCases.AddAsync(testCase, cancellationToken);
    }
    
    public async Task DeleteTestCaseAsync(Guid problemId, Guid testCaseId, CancellationToken cancellationToken = default)
    {
        var testCase = await _context.TestCases
            .FirstOrDefaultAsync(tc => tc.ProblemId == problemId && tc.Id == testCaseId, cancellationToken);
        
        if (testCase != null)
        {
            _context.TestCases.Remove(testCase);
        }
    }
    
    public async Task<IReadOnlyList<Guid>> GetSubmissionIdsAsync(Guid problemId, CancellationToken cancellationToken = default)
    {
        return await _context.Submissions
            .Where(s => s.ProblemId == problemId)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
    }
}

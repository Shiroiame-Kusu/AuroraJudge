using AuroraJudge.Application.DTOs;
using AuroraJudge.Domain.Common;
using AuroraJudge.Domain.Entities;
using AuroraJudge.Domain.Enums;
using AuroraJudge.Domain.Interfaces;
using AuroraJudge.Shared.Models;
using System.Text.RegularExpressions;

namespace AuroraJudge.Application.Services;

/// <summary>
/// Judger 调度服务接口 - 用于任务分发和 Judger 管理
/// </summary>
public interface IJudgerDispatchService
{
    // 任务管理（SubmissionService 使用）
    Task EnqueueTaskAsync(Submission submission, CancellationToken ct = default);
    Task<int> GetPendingTaskCountAsync(CancellationToken ct = default);
    
    // Judger 管理（JudgerController 使用）
    Task<JudgerNodeInfo> RegisterJudgerAsync(string name, string secret, int maxConcurrent, List<string> languages, CancellationToken ct = default);
    Task<JudgerNodeInfo?> AuthenticateJudgerAsync(Guid judgerId, string secret, CancellationToken ct = default);
    Task UpdateHeartbeatAsync(Guid judgerId, CancellationToken ct = default);
    Task<IReadOnlyList<JudgerNodeInfo>> GetAllJudgersAsync(CancellationToken ct = default);
    Task RemoveJudgerAsync(Guid judgerId, CancellationToken ct = default);
    
    // 任务获取和上报（Judger API 使用）
    Task<JudgeTaskInfo?> FetchTaskAsync(Guid judgerId, CancellationToken ct = default);
    Task ReportResultAsync(Guid judgerId, JudgeResultInfo result, CancellationToken ct = default);
}

/// <summary>
/// Judger 节点信息（API 使用）
/// </summary>
public class JudgerNodeInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MaxConcurrentTasks { get; set; }
    public int CurrentTasks { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastHeartbeat { get; set; }
    public List<string> SupportedLanguages { get; set; } = new();
}

/// <summary>
/// 评测任务信息（API 返回）
/// </summary>
public class JudgeTaskInfo
{
    public Guid TaskId { get; set; }
    public Guid SubmissionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int TimeLimit { get; set; }
    public int MemoryLimit { get; set; }
    public string JudgeMode { get; set; } = string.Empty;
    public string? SpecialJudgeCode { get; set; }
    public List<JudgeTestCaseInfo> TestCases { get; set; } = new();
}

public class JudgeTestCaseInfo
{
    public int Order { get; set; }
    public string InputPath { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public int Score { get; set; }
}

/// <summary>
/// 评测结果信息（Judger 上报）
/// </summary>
public class JudgeResultInfo
{
    public Guid SubmissionId { get; set; }
    public JudgeStatus Status { get; set; }
    public int? Score { get; set; }
    public int? TimeUsed { get; set; }
    public int? MemoryUsed { get; set; }
    public string? CompileMessage { get; set; }
    public string? JudgeMessage { get; set; }
    public List<TestCaseResultInfo> TestResults { get; set; } = new();
}

public class TestCaseResultInfo
{
    public int Order { get; set; }
    public JudgeStatus Status { get; set; }
    public int TimeUsed { get; set; }
    public int MemoryUsed { get; set; }
    public int Score { get; set; }
    public string? Message { get; set; }
}

public class SubmissionService : ISubmissionService
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IProblemRepository _problemRepository;
    private readonly IContestRepository _contestRepository;
    private readonly IJudgerDispatchService _judgerDispatchService;
    private readonly IUnitOfWork _unitOfWork;
    
    public SubmissionService(
        ISubmissionRepository submissionRepository,
        IProblemRepository problemRepository,
        IContestRepository contestRepository,
        IJudgerDispatchService judgerDispatchService,
        IUnitOfWork unitOfWork)
    {
        _submissionRepository = submissionRepository;
        _problemRepository = problemRepository;
        _contestRepository = contestRepository;
        _judgerDispatchService = judgerDispatchService;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<PagedResponse<SubmissionDto>> GetSubmissionsAsync(SubmissionQueryRequest query, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _submissionRepository.GetPagedAsync(
            query.Page,
            query.PageSize,
            query.UserId,
            query.ProblemId,
            query.ContestId,
            query.Language,
            query.Status,
            cancellationToken);
        
        var dtos = items.Select(MapToSubmissionDto).ToList();
        
        return new PagedResponse<SubmissionDto>
        {
            Items = dtos,
            Total = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
    
    public async Task<SubmissionDetailDto> GetSubmissionAsync(Guid id, Guid? requesterId, bool forceViewCode = false, CancellationToken cancellationToken = default)
    {
        var submission = await _submissionRepository.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("提交不存在");
        
        // Check permission to view code
        var canViewCode = true;
        if (requesterId.HasValue && requesterId.Value != submission.UserId)
        {
            // Check if contest allows viewing others' code
            if (submission.ContestId.HasValue)
            {
                var contest = await _contestRepository.GetByIdAsync(submission.ContestId.Value, cancellationToken);
                if (contest != null && !contest.AllowViewOthersCode && DateTime.UtcNow < contest.EndTime)
                {
                    canViewCode = false;
                }
            }
        }

        if (forceViewCode)
        {
            canViewCode = true;
        }
        
        return MapToSubmissionDetailDto(submission, canViewCode);
    }

    public async Task<IReadOnlyList<SubmissionSimilarityDto>> GetSimilarSubmissionsAsync(
        Guid submissionId,
        int top = 10,
        int candidateLimit = 500,
        CancellationToken cancellationToken = default)
    {
        if (top <= 0) top = 10;
        if (top > 50) top = 50;
        if (candidateLimit <= 0) candidateLimit = 200;
        if (candidateLimit > 5000) candidateLimit = 5000;

        var target = await _submissionRepository.GetByIdWithDetailsAsync(submissionId, cancellationToken)
            ?? throw new KeyNotFoundException("提交不存在");

        var candidates = await _submissionRepository.GetRecentForSimilarityAsync(
            target.ProblemId,
            target.Language,
            target.Id,
            candidateLimit,
            cancellationToken);

        var targetShingles = BuildShingles(target.Code);

        var results = new List<(Submission Submission, double Score)>();
        foreach (var candidate in candidates)
        {
            // Small fast-path
            if (candidate.CodeLength <= 0 || string.IsNullOrWhiteSpace(candidate.Code))
            {
                continue;
            }

            var score = Jaccard(targetShingles, BuildShingles(candidate.Code));
            results.Add((candidate, score));
        }

        return results
            .OrderByDescending(x => x.Score)
            .Take(top)
            .Select(x => new SubmissionSimilarityDto(
                SubmissionId: x.Submission.Id,
                UserId: x.Submission.UserId,
                Username: x.Submission.User?.Username ?? string.Empty,
                ProblemId: x.Submission.ProblemId,
                ProblemTitle: x.Submission.Problem?.Title ?? string.Empty,
                Language: x.Submission.Language,
                CodeLength: x.Submission.CodeLength,
                SubmittedAt: x.Submission.CreatedAt,
                Similarity: Math.Round(x.Score * 100, 2)
            ))
            .ToList();
    }
    
    public async Task<SubmissionDto> CreateSubmissionAsync(CreateSubmissionRequest request, Guid userId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        // Verify problem exists
        var problem = await _problemRepository.GetByIdAsync(request.ProblemId, cancellationToken)
            ?? throw new ValidationException("题目不存在");
        
        // Verify contest if specified
        Contest? contest = null;
        if (request.ContestId.HasValue)
        {
            contest = await _contestRepository.GetByIdAsync(request.ContestId.Value, cancellationToken)
                ?? throw new ValidationException("比赛不存在");
            
            var now = DateTime.UtcNow;
            if (now < contest.StartTime)
            {
                throw new ValidationException("比赛尚未开始");
            }
            
            if (now > contest.EndTime && !contest.AllowLateSubmission)
            {
                throw new ValidationException("比赛已结束");
            }
        }
        
        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            ProblemId = request.ProblemId,
            UserId = userId,
            ContestId = request.ContestId,
            Code = request.Code,
            Language = request.Language,
            CodeLength = request.Code.Length,
            Status = JudgeStatus.Pending,
            SubmitIp = ipAddress,
            CreatedAt = DateTime.UtcNow
        };
        
        await _submissionRepository.AddAsync(submission, cancellationToken);
        
        // Update problem submission count
        problem.SubmissionCount++;
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // 将任务入队到 JudgerDispatchService
        await _judgerDispatchService.EnqueueTaskAsync(submission, cancellationToken);
        
        // Load related data for response
        submission.Problem = problem;
        
        return MapToSubmissionDto(submission);
    }
    
    public async Task RejudgeSubmissionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var submission = await _submissionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("提交不存在");
        
        // Reset judging status
        submission.Status = JudgeStatus.Pending;
        submission.CompileMessage = null;
        submission.JudgeMessage = null;
        submission.TimeUsed = null;
        submission.MemoryUsed = null;
        submission.Score = null;
        submission.JudgedAt = null;
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // 重新入队到 JudgerDispatchService
        await _judgerDispatchService.EnqueueTaskAsync(submission, cancellationToken);
    }
    
    #region Mapping Helpers
    
    private static SubmissionDto MapToSubmissionDto(Submission submission)
    {
        return new SubmissionDto(
            Id: submission.Id,
            ProblemId: submission.ProblemId,
            ProblemTitle: submission.Problem?.Title ?? "",
            UserId: submission.UserId,
            Username: submission.User?.Username ?? "",
            ContestId: submission.ContestId,
            ContestTitle: submission.Contest?.Title,
            Language: submission.Language,
            CodeLength: submission.CodeLength,
            Status: submission.Status,
            Score: submission.Score,
            TimeUsed: submission.TimeUsed,
            MemoryUsed: submission.MemoryUsed,
            SubmittedAt: submission.CreatedAt,
            JudgedAt: submission.JudgedAt
        );
    }
    
    private static SubmissionDetailDto MapToSubmissionDetailDto(Submission submission, bool canViewCode)
    {
        var results = new List<JudgeResultDto>();
        if (submission.JudgeResults?.Any() == true)
        {
            results = submission.JudgeResults
                .OrderBy(r => r.TestCaseOrder)
                .Select(r => new JudgeResultDto(
                    TestCaseOrder: r.TestCaseOrder,
                    Subtask: r.Subtask,
                    Status: r.Status,
                    TimeUsed: r.TimeUsed,
                    MemoryUsed: r.MemoryUsed,
                    Score: r.Score,
                    Message: r.Message
                ))
                .ToList();
        }
        
        return new SubmissionDetailDto(
            Id: submission.Id,
            ProblemId: submission.ProblemId,
            ProblemTitle: submission.Problem?.Title ?? "",
            UserId: submission.UserId,
            Username: submission.User?.Username ?? "",
            ContestId: submission.ContestId,
            ContestTitle: submission.Contest?.Title,
            Code: canViewCode ? submission.Code : "*** 代码不可见 ***",
            Language: submission.Language,
            CodeLength: submission.CodeLength,
            Status: submission.Status,
            Score: submission.Score,
            TimeUsed: submission.TimeUsed,
            MemoryUsed: submission.MemoryUsed,
            CompileMessage: submission.CompileMessage,
            JudgeMessage: submission.JudgeMessage,
            SubmittedAt: submission.CreatedAt,
            JudgedAt: submission.JudgedAt,
            Results: results
        );
    }
    
    #endregion

    #region Similarity Helpers

    private static readonly Regex TokenRegex = new(@"[A-Za-z_][A-Za-z0-9_]*|\d+|\S", RegexOptions.Compiled);

    private static HashSet<int> BuildShingles(string code)
    {
        // Tokenize loosely; language-agnostic, not a full lexer.
        var tokens = TokenRegex.Matches(code)
            .Select(m => m.Value)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        const int k = 5;
        var set = new HashSet<int>();

        if (tokens.Count == 0)
        {
            return set;
        }

        if (tokens.Count < k)
        {
            set.Add(HashTokens(tokens));
            return set;
        }

        for (var i = 0; i <= tokens.Count - k; i++)
        {
            set.Add(HashTokens(tokens, i, k));
        }

        return set;
    }

    private static int HashTokens(List<string> tokens)
        => HashTokens(tokens, 0, tokens.Count);

    private static int HashTokens(List<string> tokens, int start, int count)
    {
        unchecked
        {
            var hash = 17;
            for (var i = 0; i < count; i++)
            {
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(tokens[start + i]);
            }
            return hash;
        }
    }

    private static double Jaccard(HashSet<int> a, HashSet<int> b)
    {
        if (a.Count == 0 && b.Count == 0) return 1;
        if (a.Count == 0 || b.Count == 0) return 0;

        // Iterate smaller for intersection count.
        var (small, large) = a.Count <= b.Count ? (a, b) : (b, a);
        var intersection = 0;
        foreach (var x in small)
        {
            if (large.Contains(x)) intersection++;
        }

        var union = a.Count + b.Count - intersection;
        return union == 0 ? 0 : (double)intersection / union;
    }

    #endregion
}

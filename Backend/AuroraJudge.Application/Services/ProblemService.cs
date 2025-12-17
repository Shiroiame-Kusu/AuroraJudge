using AuroraJudge.Application.DTOs;
using AuroraJudge.Domain.Common;
using AuroraJudge.Domain.Entities;
using AuroraJudge.Domain.Enums;
using AuroraJudge.Domain.Interfaces;
using AuroraJudge.Shared.Constants;
using AuroraJudge.Shared.Models;
using Microsoft.AspNetCore.Http;

namespace AuroraJudge.Application.Services;

public class ProblemService : IProblemService
{
    private readonly IProblemRepository _problemRepository;
    private readonly IContestRepository _contestRepository;
    private readonly IStorageService _storageService;
    private readonly IPermissionService _permissionService;
    private readonly IUnitOfWork _unitOfWork;
    
    public ProblemService(
        IProblemRepository problemRepository,
        IContestRepository contestRepository,
        IStorageService storageService,
        IPermissionService permissionService,
        IUnitOfWork unitOfWork)
    {
        _problemRepository = problemRepository;
        _contestRepository = contestRepository;
        _storageService = storageService;
        _permissionService = permissionService;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<PagedResponse<ProblemListDto>> GetProblemsAsync(
        int page, 
        int pageSize, 
        string? search, 
        Guid? tagId, 
        int? difficulty, 
        Guid? userId, 
        CancellationToken cancellationToken = default)
    {
        ProblemDifficulty? diffEnum = difficulty.HasValue ? (ProblemDifficulty)difficulty.Value : null;

        var canViewHidden = userId.HasValue &&
            await _permissionService.HasPermissionAsync(userId.Value, Permissions.ProblemViewHidden, cancellationToken);

        var (items, totalCount) = await _problemRepository.GetPagedForViewerAsync(
            page, pageSize, search, tagId, diffEnum, userId, canViewHidden, cancellationToken);

        IReadOnlySet<Guid>? solvedSet = null;
        if (userId.HasValue)
        {
            var ids = items.Select(p => p.Id).ToList();
            solvedSet = await _problemRepository.GetSolvedProblemIdsAsync(userId.Value, ids, cancellationToken);
        }

        var dtos = items.Select(p => MapToProblemListDto(p, solvedSet?.Contains(p.Id))).ToList();
        
        return new PagedResponse<ProblemListDto>
        {
            Items = dtos,
            Total = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProblemDto> GetProblemAsync(Guid id, Guid? userId, Guid? contestId = null, CancellationToken cancellationToken = default)
    {
        var problem = await _problemRepository.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("题目不存在");

        var canViewHidden = userId.HasValue &&
            await _permissionService.HasPermissionAsync(userId.Value, Permissions.ProblemViewHidden, cancellationToken);

        await EnsureCanViewProblemAsync(problem, userId, canViewHidden, contestId, cancellationToken);
        
        return MapToProblemDto(problem);
    }

    public async Task<IReadOnlyList<TagDto>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        var tags = await _problemRepository.GetAllTagsAsync(cancellationToken);
        return tags
            .Select(t => new TagDto(
                Id: t.Id,
                Name: t.Name,
                Color: t.Color,
                Category: t.Category,
                UsageCount: t.UsageCount
            ))
            .ToList();
    }

    public async Task<TagDto> CreateTagAsync(CreateTagRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("标签名称不能为空");
        }

        var existing = await _problemRepository.GetTagByNameAsync(request.Name.Trim(), cancellationToken);
        if (existing != null)
        {
            throw new ConflictException("标签已存在");
        }

        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Color = string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim(),
            UsageCount = 0,
        };

        await _problemRepository.AddTagAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TagDto(tag.Id, tag.Name, tag.Color, tag.Category, tag.UsageCount);
    }

    public async Task<TagDto> UpdateTagAsync(Guid tagId, CreateTagRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("标签名称不能为空");
        }

        var tag = await _problemRepository.GetTagByIdAsync(tagId, cancellationToken)
            ?? throw new KeyNotFoundException("标签不存在");

        var normalizedName = request.Name.Trim();
        var nameConflict = await _problemRepository.GetTagByNameAsync(normalizedName, cancellationToken);
        if (nameConflict != null && nameConflict.Id != tagId)
        {
            throw new ConflictException("标签名称已被占用");
        }

        tag.Name = normalizedName;
        tag.Color = string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.Trim();
        tag.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TagDto(tag.Id, tag.Name, tag.Color, tag.Category, tag.UsageCount);
    }

    public async Task DeleteTagAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        await _problemRepository.DeleteTagAsync(tagId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<ProblemDto> CreateProblemAsync(CreateProblemRequest request, Guid creatorId, CancellationToken cancellationToken = default)
    {
        var problem = new Problem
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            InputFormat = request.InputFormat,
            OutputFormat = request.OutputFormat,
            SampleInput = request.SampleInput,
            SampleOutput = request.SampleOutput,
            Hint = request.Hint,
            Source = request.Source,
            TimeLimit = request.TimeLimit,
            MemoryLimit = request.MemoryLimit,
            StackLimit = request.StackLimit,
            OutputLimit = request.OutputLimit,
            JudgeMode = request.JudgeMode,
            SpecialJudgeCode = request.SpecialJudgeCode,
            SpecialJudgeLanguage = request.SpecialJudgeLanguage,
            AllowedLanguages = request.AllowedLanguages,
            Visibility = request.Visibility,
            Difficulty = request.Difficulty,
            CreatorId = creatorId,
            CreatedAt = DateTime.UtcNow
        };
        
        // Add tags
        if (request.TagIds?.Any() == true)
        {
            foreach (var tagId in request.TagIds)
            {
                problem.ProblemTags.Add(new ProblemTag { ProblemId = problem.Id, TagId = tagId });
            }
        }
        
        await _problemRepository.AddAsync(problem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return MapToProblemDto(problem);
    }
    
    public async Task<ProblemDto> UpdateProblemAsync(Guid id, UpdateProblemRequest request, CancellationToken cancellationToken = default)
    {
        var problem = await _problemRepository.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("题目不存在");
        
        problem.Title = request.Title;
        problem.Description = request.Description;
        problem.InputFormat = request.InputFormat;
        problem.OutputFormat = request.OutputFormat;
        problem.SampleInput = request.SampleInput;
        problem.SampleOutput = request.SampleOutput;
        problem.Hint = request.Hint;
        problem.Source = request.Source;
        problem.TimeLimit = request.TimeLimit;
        problem.MemoryLimit = request.MemoryLimit;
        problem.StackLimit = request.StackLimit;
        problem.OutputLimit = request.OutputLimit;
        problem.JudgeMode = request.JudgeMode;
        problem.SpecialJudgeCode = request.SpecialJudgeCode;
        problem.SpecialJudgeLanguage = request.SpecialJudgeLanguage;
        problem.AllowedLanguages = request.AllowedLanguages;
        problem.Visibility = request.Visibility;
        problem.Difficulty = request.Difficulty;
        problem.UpdatedAt = DateTime.UtcNow;
        
        // Update tags
        problem.ProblemTags.Clear();
        if (request.TagIds?.Any() == true)
        {
            foreach (var tagId in request.TagIds)
            {
                problem.ProblemTags.Add(new ProblemTag { ProblemId = problem.Id, TagId = tagId });
            }
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return MapToProblemDto(problem);
    }
    
    public async Task DeleteProblemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var problem = await _problemRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("题目不存在");
        
        problem.IsDeleted = true;
        problem.DeletedAt = DateTime.UtcNow;
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<TestCaseDto>> GetTestCasesAsync(Guid problemId, CancellationToken cancellationToken = default)
    {
        var testCases = await _problemRepository.GetTestCasesAsync(problemId, cancellationToken);
        return testCases.OrderBy(tc => tc.Order).Select(MapToTestCaseDto).ToList();
    }
    
    public async Task<TestCaseDto> AddTestCaseAsync(
        Guid problemId, 
        CreateTestCaseRequest request, 
        IFormFile inputFile, 
        IFormFile outputFile, 
        CancellationToken cancellationToken = default)
    {
        var problem = await _problemRepository.GetByIdAsync(problemId, cancellationToken)
            ?? throw new KeyNotFoundException("题目不存在");
        
        // Save files
        var inputPath = $"testcases/{problemId}/input_{request.Order}.txt";
        var outputPath = $"testcases/{problemId}/output_{request.Order}.txt";
        
        await using (var inputStream = inputFile.OpenReadStream())
        {
            await _storageService.UploadAsync(inputPath, inputStream, cancellationToken);
        }
        
        await using (var outputStream = outputFile.OpenReadStream())
        {
            await _storageService.UploadAsync(outputPath, outputStream, cancellationToken);
        }
        
        var testCase = new TestCase
        {
            Id = Guid.NewGuid(),
            ProblemId = problemId,
            Order = request.Order,
            InputPath = inputPath,
            OutputPath = outputPath,
            InputSize = inputFile.Length,
            OutputSize = outputFile.Length,
            Score = request.Score,
            IsSample = request.IsSample,
            Subtask = request.Subtask,
            Description = request.Description
        };
        
        await _problemRepository.AddTestCaseAsync(testCase, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return MapToTestCaseDto(testCase);
    }
    
    public async Task DeleteTestCaseAsync(Guid problemId, Guid testCaseId, CancellationToken cancellationToken = default)
    {
        await _problemRepository.DeleteTestCaseAsync(problemId, testCaseId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<(Stream Stream, string FileName)> DownloadTestCaseFileAsync(
        Guid problemId,
        Guid testCaseId,
        bool isInput,
        CancellationToken cancellationToken = default)
    {
        var tc = await _problemRepository.GetTestCaseByIdAsync(problemId, testCaseId, cancellationToken)
            ?? throw new KeyNotFoundException("测试用例不存在");

        var path = isInput ? tc.InputPath : tc.OutputPath;
        var stream = await _storageService.DownloadAsync(path, cancellationToken);

        var suffix = isInput ? "input" : "output";
        var fileName = $"{problemId}_tc{tc.Order}_{suffix}.txt";

        return (stream, fileName);
    }
    
    public async Task RejudgeProblemAsync(Guid problemId, CancellationToken cancellationToken = default)
    {
        var problem = await _problemRepository.GetByIdAsync(problemId, cancellationToken)
            ?? throw new KeyNotFoundException("题目不存在");
        
        // Get all submission IDs for this problem and mark them for rejudging
        var submissionIds = await _problemRepository.GetSubmissionIdsAsync(problemId, cancellationToken);
        
        // Note: Would need to add rejudge logic here via message queue
        await Task.CompletedTask;
    }
    
    #region Mapping Helpers

    private async Task EnsureCanViewProblemAsync(
        Problem problem,
        Guid? userId,
        bool canViewHidden,
        Guid? contestId,
        CancellationToken cancellationToken)
    {
        if (canViewHidden)
        {
            return;
        }

        if (problem.Visibility == ProblemVisibility.Public)
        {
            return;
        }

        // Creator can always view their own problems.
        if (userId.HasValue && problem.CreatorId == userId.Value)
        {
            return;
        }

        if (problem.Visibility == ProblemVisibility.ContestOnly)
        {
            if (!contestId.HasValue)
            {
                throw new ForbiddenException("该题目仅在比赛中可见");
            }

            var contest = await _contestRepository.GetByIdWithDetailsAsync(contestId.Value, cancellationToken)
                ?? throw new KeyNotFoundException("比赛不存在");

            var inContest = contest.ContestProblems?.Any(cp => cp.ProblemId == problem.Id) == true;
            if (!inContest)
            {
                throw new ForbiddenException("该题目不属于指定比赛");
            }

            var now = DateTime.UtcNow;

            // Before start: only registered users can't view; require elevated access.
            if (now < contest.StartTime)
            {
                throw new ForbiddenException("比赛尚未开始，题目不可见");
            }

            // During contest: require registration.
            if (now <= contest.EndTime)
            {
                if (!userId.HasValue)
                {
                    throw new ForbiddenException("请先报名比赛后查看题目");
                }

                var isRegistered = await _contestRepository.IsUserRegisteredAsync(contest.Id, userId.Value, cancellationToken);
                if (!isRegistered)
                {
                    throw new ForbiddenException("请先报名比赛后查看题目");
                }

                return;
            }

            // After contest: allow only when problems are published.
            if (!contest.PublishProblemsAfterEnd)
            {
                throw new ForbiddenException("比赛未公开题目");
            }

            return;
        }

        // Private / Hidden (and any future non-public visibility)
        throw new ForbiddenException("无权访问该题目");
    }
    
    private static ProblemDto MapToProblemDto(Problem problem)
    {
        var tags = problem.ProblemTags?
            .Select(pt => new TagDto(
                Id: pt.Tag?.Id ?? pt.TagId,
                Name: pt.Tag?.Name ?? "",
                Color: pt.Tag?.Color,
                Category: pt.Tag?.Category,
                UsageCount: pt.Tag?.UsageCount ?? 0
            ))
            .ToList() ?? [];
        
        return new ProblemDto(
            Id: problem.Id,
            Title: problem.Title,
            Description: problem.Description,
            InputFormat: problem.InputFormat,
            OutputFormat: problem.OutputFormat,
            SampleInput: problem.SampleInput,
            SampleOutput: problem.SampleOutput,
            Hint: problem.Hint,
            Source: problem.Source,
            TimeLimit: problem.TimeLimit,
            MemoryLimit: problem.MemoryLimit,
            StackLimit: problem.StackLimit,
            OutputLimit: problem.OutputLimit,
            JudgeMode: problem.JudgeMode,
            SpecialJudgeCode: problem.SpecialJudgeCode,
            SpecialJudgeLanguage: problem.SpecialJudgeLanguage,
            AllowedLanguages: problem.AllowedLanguages,
            Visibility: problem.Visibility,
            Difficulty: problem.Difficulty,
            SubmissionCount: problem.SubmissionCount,
            AcceptedCount: problem.AcceptedCount,
            AcceptRate: problem.SubmissionCount > 0 ? (double)problem.AcceptedCount / problem.SubmissionCount : 0,
            Tags: tags,
            CreatedAt: problem.CreatedAt
        );
    }
    
    private static ProblemListDto MapToProblemListDto(Problem problem, bool? solved)
    {
        var tags = problem.ProblemTags?
            .Select(pt => new TagDto(
                Id: pt.Tag?.Id ?? pt.TagId,
                Name: pt.Tag?.Name ?? "",
                Color: pt.Tag?.Color,
                Category: pt.Tag?.Category,
                UsageCount: pt.Tag?.UsageCount ?? 0
            ))
            .ToList() ?? [];
        
        return new ProblemListDto(
            Id: problem.Id,
            Title: problem.Title,
            Difficulty: problem.Difficulty,
            SubmissionCount: problem.SubmissionCount,
            AcceptedCount: problem.AcceptedCount,
            AcceptRate: problem.SubmissionCount > 0 ? (double)problem.AcceptedCount / problem.SubmissionCount : 0,
            Tags: tags,
            Solved: solved
        );
    }
    
    private static TestCaseDto MapToTestCaseDto(TestCase testCase)
    {
        return new TestCaseDto(
            Id: testCase.Id,
            Order: testCase.Order,
            InputSize: testCase.InputSize,
            OutputSize: testCase.OutputSize,
            Score: testCase.Score,
            IsSample: testCase.IsSample,
            Subtask: testCase.Subtask,
            Description: testCase.Description
        );
    }
    
    #endregion
}

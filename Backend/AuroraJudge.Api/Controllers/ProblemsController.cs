using AuroraJudge.Application.DTOs;
using AuroraJudge.Application.Services;
using AuroraJudge.Shared.Constants;
using AuroraJudge.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuroraJudge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProblemsController : ControllerBase
{
    private readonly IProblemService _problemService;
    private readonly ILogger<ProblemsController> _logger;
    
    public ProblemsController(IProblemService problemService, ILogger<ProblemsController> logger)
    {
        _problemService = problemService;
        _logger = logger;
    }
    
    /// <summary>
    /// 获取题目列表
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ProblemListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResponse<ProblemListDto>>>> GetProblems(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? tagId = null,
        [FromQuery] int? difficulty = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
        var result = await _problemService.GetProblemsAsync(page, pageSize, search, tagId, difficulty, userId, cancellationToken);
        return Ok(ApiResponse<PagedResponse<ProblemListDto>>.Ok(result));
    }
    
    /// <summary>
    /// 获取题目详情
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProblemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProblemDto>>> GetProblem(
        Guid id,
        [FromQuery] Guid? contestId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
        var result = await _problemService.GetProblemAsync(id, userId, contestId, cancellationToken);
        return Ok(ApiResponse<ProblemDto>.Ok(result));
    }

    /// <summary>
    /// 获取题目标签列表
    /// </summary>
    [HttpGet("tags")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TagDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TagDto>>>> GetTags(CancellationToken cancellationToken)
    {
        var result = await _problemService.GetTagsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TagDto>>.Ok(result));
    }

    /// <summary>
    /// 创建题目标签
    /// </summary>
    [Authorize]
    [HttpPost("tags")]
    [RequirePermission(Permissions.TagCreate)]
    [ProducesResponseType(typeof(ApiResponse<TagDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<TagDto>>> CreateTag([FromBody] CreateTagRequest request, CancellationToken cancellationToken)
    {
        var result = await _problemService.CreateTagAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<TagDto>.Ok(result, "标签创建成功"));
    }

    /// <summary>
    /// 更新题目标签
    /// </summary>
    [Authorize]
    [HttpPut("tags/{id:guid}")]
    [RequirePermission(Permissions.TagEdit)]
    [ProducesResponseType(typeof(ApiResponse<TagDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TagDto>>> UpdateTag(Guid id, [FromBody] CreateTagRequest request, CancellationToken cancellationToken)
    {
        var result = await _problemService.UpdateTagAsync(id, request, cancellationToken);
        return Ok(ApiResponse<TagDto>.Ok(result, "标签更新成功"));
    }

    /// <summary>
    /// 删除题目标签
    /// </summary>
    [Authorize]
    [HttpDelete("tags/{id:guid}")]
    [RequirePermission(Permissions.TagDelete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteTag(Guid id, CancellationToken cancellationToken)
    {
        await _problemService.DeleteTagAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("标签删除成功"));
    }
    
    /// <summary>
    /// 创建题目
    /// </summary>
    [Authorize]
    [HttpPost]
    [RequirePermission(Permissions.ProblemCreate)]
    [ProducesResponseType(typeof(ApiResponse<ProblemDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ProblemDto>>> CreateProblem([FromBody] CreateProblemRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await _problemService.CreateProblemAsync(request, userId, cancellationToken);
        return CreatedAtAction(nameof(GetProblem), new { id = result.Id }, ApiResponse<ProblemDto>.Ok(result, "题目创建成功"));
    }
    
    /// <summary>
    /// 更新题目
    /// </summary>
    [Authorize]
    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.ProblemEdit)]
    [ProducesResponseType(typeof(ApiResponse<ProblemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProblemDto>>> UpdateProblem(Guid id, [FromBody] UpdateProblemRequest request, CancellationToken cancellationToken)
    {
        var result = await _problemService.UpdateProblemAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ProblemDto>.Ok(result, "题目更新成功"));
    }
    
    /// <summary>
    /// 删除题目
    /// </summary>
    [Authorize]
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.ProblemDelete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteProblem(Guid id, CancellationToken cancellationToken)
    {
        await _problemService.DeleteProblemAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("题目删除成功"));
    }
    
    /// <summary>
    /// 获取题目测试用例
    /// </summary>
    [Authorize]
    [HttpGet("{id:guid}/testcases")]
    [RequirePermission(Permissions.ProblemManageTestCases)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TestCaseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TestCaseDto>>>> GetTestCases(Guid id, CancellationToken cancellationToken)
    {
        var result = await _problemService.GetTestCasesAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TestCaseDto>>.Ok(result));
    }
    
    /// <summary>
    /// 上传测试用例
    /// </summary>
    [Authorize]
    [HttpPost("{id:guid}/testcases")]
    [RequirePermission(Permissions.ProblemManageTestCases)]
    [ProducesResponseType(typeof(ApiResponse<TestCaseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<TestCaseDto>>> UploadTestCase(
        Guid id,
        [FromForm] CreateTestCaseRequest request,
        [FromForm] IFormFile inputFile,
        [FromForm] IFormFile outputFile,
        CancellationToken cancellationToken)
    {
        var result = await _problemService.AddTestCaseAsync(id, request, inputFile, outputFile, cancellationToken);
        return CreatedAtAction(nameof(GetTestCases), new { id }, ApiResponse<TestCaseDto>.Ok(result, "测试用例上传成功"));
    }
    
    /// <summary>
    /// 删除测试用例
    /// </summary>
    [Authorize]
    [HttpDelete("{problemId:guid}/testcases/{testCaseId:guid}")]
    [RequirePermission(Permissions.ProblemManageTestCases)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteTestCase(Guid problemId, Guid testCaseId, CancellationToken cancellationToken)
    {
        await _problemService.DeleteTestCaseAsync(problemId, testCaseId, cancellationToken);
        return Ok(ApiResponse.Ok("测试用例删除成功"));
    }

    /// <summary>
    /// 下载测试用例文件（仅管理员）
    /// </summary>
    [Authorize]
    [HttpGet("{problemId:guid}/testcases/{testCaseId:guid}/download")]
    [RequireRole(Roles.Admin, Roles.SuperAdmin)]
    public async Task<IActionResult> DownloadTestCaseFile(
        Guid problemId,
        Guid testCaseId,
        [FromQuery] string type = "input",
        CancellationToken cancellationToken = default)
    {
        var isInput = !string.Equals(type, "output", StringComparison.OrdinalIgnoreCase);
        var (stream, fileName) = await _problemService.DownloadTestCaseFileAsync(problemId, testCaseId, isInput, cancellationToken);
        return File(stream, "application/octet-stream", fileName);
    }
    
    /// <summary>
    /// 重判题目
    /// </summary>
    [Authorize]
    [HttpPost("{id:guid}/rejudge")]
    [RequirePermission(Permissions.ProblemRejudge)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> RejudgeProblem(Guid id, CancellationToken cancellationToken)
    {
        await _problemService.RejudgeProblemAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("已发起重判"));
    }
}

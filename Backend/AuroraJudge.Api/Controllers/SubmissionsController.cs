using AuroraJudge.Application.DTOs;
using AuroraJudge.Application.Services;
using AuroraJudge.Domain.Interfaces;
using AuroraJudge.Shared.Constants;
using AuroraJudge.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuroraJudge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubmissionsController : ControllerBase
{
    private readonly ISubmissionService _submissionService;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<SubmissionsController> _logger;
    
    public SubmissionsController(ISubmissionService submissionService, IPermissionService permissionService, ILogger<SubmissionsController> logger)
    {
        _submissionService = submissionService;
        _permissionService = permissionService;
        _logger = logger;
    }
    
    /// <summary>
    /// 获取提交列表
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<SubmissionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResponse<SubmissionDto>>>> GetSubmissions(
        [FromQuery] SubmissionQueryRequest query,
        CancellationToken cancellationToken)
    {
        var result = await _submissionService.GetSubmissionsAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResponse<SubmissionDto>>.Ok(result));
    }
    
    /// <summary>
    /// 获取提交详情
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SubmissionDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SubmissionDetailDto>>> GetSubmission(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
        var forceViewCode = false;
        if (userId.HasValue)
        {
            forceViewCode = await _permissionService.HasPermissionAsync(userId.Value, Permissions.SubmissionViewCode, cancellationToken);
        }
        var result = await _submissionService.GetSubmissionAsync(id, userId, forceViewCode, cancellationToken);
        return Ok(ApiResponse<SubmissionDetailDto>.Ok(result));
    }

    /// <summary>
    /// 相似度检测（与同题同语言的最近提交比较）
    /// </summary>
    [Authorize]
    [HttpGet("{id:guid}/similarity")]
    [RequirePermission(Permissions.SubmissionViewCode)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SubmissionSimilarityDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SubmissionSimilarityDto>>>> GetSimilarity(
        Guid id,
        [FromQuery] int top = 10,
        [FromQuery] int candidateLimit = 500,
        CancellationToken cancellationToken = default)
    {
        var result = await _submissionService.GetSimilarSubmissionsAsync(id, top, candidateLimit, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SubmissionSimilarityDto>>.Ok(result));
    }
    
    /// <summary>
    /// 提交代码
    /// </summary>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SubmissionDto>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SubmissionDto>>> CreateSubmission([FromBody] CreateSubmissionRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _submissionService.CreateSubmissionAsync(request, userId, ipAddress, cancellationToken);
        return AcceptedAtAction(nameof(GetSubmission), new { id = result.Id }, ApiResponse<SubmissionDto>.Ok(result, "提交成功"));
    }
    
    /// <summary>
    /// 重判提交
    /// </summary>
    [Authorize]
    [HttpPost("{id:guid}/rejudge")]
    [RequirePermission(Permissions.SubmissionRejudge)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> RejudgeSubmission(Guid id, CancellationToken cancellationToken)
    {
        await _submissionService.RejudgeSubmissionAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("已发起重判"));
    }
    
    /// <summary>
    /// 获取我的提交
    /// </summary>
    [Authorize]
    [HttpGet("my")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<SubmissionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResponse<SubmissionDto>>>> GetMySubmissions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? problemId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var query = new SubmissionQueryRequest
        {
            Page = page,
            PageSize = pageSize,
            UserId = userId,
            ProblemId = problemId
        };
        var result = await _submissionService.GetSubmissionsAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResponse<SubmissionDto>>.Ok(result));
    }
}

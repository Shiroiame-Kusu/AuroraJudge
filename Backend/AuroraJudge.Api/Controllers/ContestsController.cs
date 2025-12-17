using AuroraJudge.Application.DTOs;
using AuroraJudge.Application.Services;
using AuroraJudge.Shared.Constants;
using AuroraJudge.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuroraJudge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContestsController : ControllerBase
{
    private readonly IContestService _contestService;
    private readonly ILogger<ContestsController> _logger;
    
    public ContestsController(IContestService contestService, ILogger<ContestsController> logger)
    {
        _contestService = contestService;
        _logger = logger;
    }
    
    /// <summary>
    /// 获取比赛列表
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ContestDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResponse<ContestDto>>>> GetContests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null,
        [FromQuery] int? type = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _contestService.GetContestsAsync(page, pageSize, status, type, cancellationToken);
        return Ok(ApiResponse<PagedResponse<ContestDto>>.Ok(result));
    }
    
    /// <summary>
    /// 获取比赛详情
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ContestDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ContestDetailDto>>> GetContest(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
        var result = await _contestService.GetContestAsync(id, userId, cancellationToken);
        return Ok(ApiResponse<ContestDetailDto>.Ok(result));
    }
    
    /// <summary>
    /// 创建比赛
    /// </summary>
    [Authorize]
    [HttpPost]
    [RequirePermission(Permissions.ContestCreate)]
    [ProducesResponseType(typeof(ApiResponse<ContestDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ContestDto>>> CreateContest([FromBody] CreateContestRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await _contestService.CreateContestAsync(request, userId, cancellationToken);
        return CreatedAtAction(nameof(GetContest), new { id = result.Id }, ApiResponse<ContestDto>.Ok(result, "比赛创建成功"));
    }
    
    /// <summary>
    /// 更新比赛
    /// </summary>
    [Authorize]
    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.ContestEdit)]
    [ProducesResponseType(typeof(ApiResponse<ContestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ContestDto>>> UpdateContest(Guid id, [FromBody] UpdateContestRequest request, CancellationToken cancellationToken)
    {
        var result = await _contestService.UpdateContestAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ContestDto>.Ok(result, "比赛更新成功"));
    }
    
    /// <summary>
    /// 删除比赛
    /// </summary>
    [Authorize]
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.ContestDelete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteContest(Guid id, CancellationToken cancellationToken)
    {
        await _contestService.DeleteContestAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("比赛删除成功"));
    }
    
    /// <summary>
    /// 报名比赛
    /// </summary>
    [Authorize]
    [HttpPost("{id:guid}/register")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> RegisterContest(Guid id, [FromBody] ContestRegisterRequest? request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        await _contestService.RegisterContestAsync(id, userId, request?.Password, cancellationToken);
        return Ok(ApiResponse.Ok("报名成功"));
    }
    
    /// <summary>
    /// 取消报名
    /// </summary>
    [Authorize]
    [HttpDelete("{id:guid}/register")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> UnregisterContest(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        await _contestService.UnregisterContestAsync(id, userId, cancellationToken);
        return Ok(ApiResponse.Ok("已取消报名"));
    }
    
    /// <summary>
    /// 获取排行榜
    /// </summary>
    [HttpGet("{id:guid}/standings")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ContestRankingDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ContestRankingDto>>>> GetStandings(Guid id, CancellationToken cancellationToken)
    {
        var result = await _contestService.GetStandingsAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ContestRankingDto>>.Ok(result));
    }
    
    /// <summary>
    /// 获取比赛题目
    /// </summary>
    [HttpGet("{id:guid}/problems")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ContestProblemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ContestProblemDto>>>> GetContestProblems(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
        var result = await _contestService.GetContestProblemsAsync(id, userId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ContestProblemDto>>.Ok(result));
    }
    
    /// <summary>
    /// 添加/更新比赛题目
    /// </summary>
    [Authorize]
    [HttpPut("{id:guid}/problems")]
    [RequirePermission(Permissions.ContestEdit)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> UpdateContestProblems(Guid id, [FromBody] IReadOnlyList<ContestProblemRequest> problems, CancellationToken cancellationToken)
    {
        await _contestService.UpdateContestProblemsAsync(id, problems, cancellationToken);
        return Ok(ApiResponse.Ok("比赛题目更新成功"));
    }
    
    /// <summary>
    /// 获取比赛公告
    /// </summary>
    [HttpGet("{id:guid}/announcements")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ContestAnnouncementDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ContestAnnouncementDto>>>> GetContestAnnouncements(Guid id, CancellationToken cancellationToken)
    {
        var result = await _contestService.GetContestAnnouncementsAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ContestAnnouncementDto>>.Ok(result));
    }
    
    /// <summary>
    /// 创建比赛公告
    /// </summary>
    [Authorize]
    [HttpPost("{id:guid}/announcements")]
    [RequirePermission(Permissions.ContestAnnouncement)]
    [ProducesResponseType(typeof(ApiResponse<ContestAnnouncementDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ContestAnnouncementDto>>> CreateContestAnnouncement(Guid id, [FromBody] CreateContestAnnouncementRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await _contestService.CreateAnnouncementAsync(id, request, userId, cancellationToken);
        return CreatedAtAction(nameof(GetContestAnnouncements), new { id }, ApiResponse<ContestAnnouncementDto>.Ok(result, "公告发布成功"));
    }
}

using AuroraJudge.Application.DTOs;
using AuroraJudge.Application.Services;
using AuroraJudge.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuroraJudge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RankingsController : ControllerBase
{
    private readonly IRankingService _rankingService;

    public RankingsController(IRankingService rankingService)
    {
        _rankingService = rankingService;
    }

    /// <summary>
    /// 获取全站排行榜
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<RankingUserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResponse<RankingUserDto>>>> GetRankings(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _rankingService.GetRankingsAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResponse<RankingUserDto>>.Ok(result));
    }
}

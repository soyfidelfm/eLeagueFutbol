using eLeagueFutbol.Application.Stats;
using Microsoft.AspNetCore.Mvc;

namespace eLeagueFutbol.API.Controllers;

[ApiController]
[Route("api/stats")]
public class StatsController : ControllerBase
{
	private readonly IStatsService _statsService;

	public StatsController(IStatsService statsService)
	{
		_statsService = statsService;
	}

	[HttpGet("overview")]
	public async Task<IActionResult> GetOverview()
	{
		var result = await _statsService.GetOverviewAsync();
		return Ok(result);
	}
}
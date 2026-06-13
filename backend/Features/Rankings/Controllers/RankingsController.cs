using backend.Features.Rankings.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Features.Rankings.Controllers
{
	[ApiController]
	[Route("api/rankings")]
	public sealed class RankingsController : ControllerBase
	{
		private readonly IRankingService _rankingService;

		public RankingsController(IRankingService rankingService)
		{
			_rankingService = rankingService;
		}

		[HttpGet]
		public async Task<IActionResult> GetRankings([FromQuery] Guid? seasonId)
		{
			var response = await _rankingService.GetRankingsAsync(seasonId);
			return Ok(response);
		}
	}
}

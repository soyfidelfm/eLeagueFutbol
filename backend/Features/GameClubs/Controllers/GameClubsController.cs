using backend.Features.GameClubs.Dtos;
using backend.Features.GameClubs.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Features.GameClubs.Controllers;

[ApiController]
[Route("api/game-clubs")]
public class GameClubsController : ControllerBase
{
	private readonly IGameClubService _gameClubService;

	public GameClubsController(IGameClubService gameClubService)
	{
		_gameClubService = gameClubService;
	}

	[HttpGet]
	public async Task<ActionResult<List<GameClubDto>>> GetAll()
	{
		var clubs = await _gameClubService.GetAllAsync();

		return Ok(clubs);
	}

	[HttpPost("import")]
	public async Task<IActionResult> Import(ImportGameClubsDto dto)
	{
		var result = await _gameClubService.ImportAsync(dto);

		return Ok(result);
	}

	[HttpPost("sync-cmtracker")]
	public async Task<IActionResult> SyncCmTracker()
	{
		var result =
			await _gameClubService.SyncFromCmTrackerAsync();

		return Ok(result);
	}
	[HttpGet("leagues")]
	public async Task<ActionResult<List<string>>> GetLeagues()
	{
		var leagues = await _gameClubService.GetLeaguesAsync();

		return Ok(leagues);
	}

	[HttpGet("by-league")]
	public async Task<ActionResult<List<GameClubDto>>> GetByLeague(
		[FromQuery] string league)
	{
		var clubs = await _gameClubService.GetByLeagueAsync(league);

		return Ok(clubs);
	}
}
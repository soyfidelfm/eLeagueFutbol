using backend.Features.Cloudinary.Services;
using backend.Features.GameData.Dtos;
using backend.Features.GameData.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.GameData.Controllers;

[ApiController]
[Route("api/game-data")]
public class GameDataController : ControllerBase
{
	private readonly IGameDataService _gameDataService;
	private readonly ICloudinaryService _cloudinaryService;

	public GameDataController(IGameDataService gameDataService, ICloudinaryService cloudinaryService)
	{
		_gameDataService = gameDataService;
		_cloudinaryService = cloudinaryService;
	}

	[HttpGet("clubs")]
	public async Task<ActionResult<List<GameClubDto>>> GetAll()
	{
		var clubs = await _gameDataService.GetAllAsync();
		return Ok(clubs);
	}

	[HttpPost("clubs/import")]
	public async Task<IActionResult> Import(ImportGameClubsDto dto)
	{
		var result = await _gameDataService.ImportAsync(dto);
		return Ok(result);
	}

	[HttpPost("clubs/sync-cmtracker")]
	public async Task<IActionResult> SyncCmTracker()
	{
		var result = await _gameDataService.SyncFromCmTrackerAsync();
		return Ok(result);
	}

	[HttpGet("clubs/leagues")]
	public async Task<ActionResult<List<string>>> GetLeagues()
	{
		var leagues = await _gameDataService.GetLeaguesAsync();
		return Ok(leagues);
	}

	[HttpGet("clubs/by-league")]
	public async Task<ActionResult<List<GameClubDto>>> GetByLeague([FromQuery] string league)
	{
		var clubs = await _gameDataService.GetByLeagueAsync(league);
		return Ok(clubs);
	}

	[HttpPost("players/import-fc26-csv")]
	public async Task<IActionResult> ImportPlayersFromFc26Csv(
	IFormFile file)
	{
		var result =
			await _gameDataService.ImportPlayersFromFc26CsvAsync(file);

		return Ok(result);
	}

	[HttpGet("players")]
	public async Task<ActionResult<List<GamePlayerDto>>> GetPlayers([FromQuery] string? search)
	{
		var players = await _gameDataService.GetPlayersAsync(search);
		return Ok(players);
	}

	[HttpPost("{gameClubId:guid}/logo")]
	public async Task<IActionResult> UploadLogo(Guid gameClubId, IFormFile file)
	{
		var result = await _gameDataService.UploadLogoAsync(gameClubId, file);

		if (result is null)
			return NotFound("Club not found.");

		return Ok(result);
	}
}
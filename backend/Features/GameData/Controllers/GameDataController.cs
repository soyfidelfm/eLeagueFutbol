using System.Security.Claims;
using backend.Data;
using backend.Features.Cloudinary.Services;
using backend.Features.GameData.Dtos;
using backend.Features.GameData.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.GameData.Controllers;

[ApiController]
[Route("api/game-data")]
public class GameDataController : ControllerBase
{
	private readonly IGameDataService _gameDataService;
	private readonly ICloudinaryService _cloudinaryService;
	private readonly AppDbContext _context;

	public GameDataController(
		IGameDataService gameDataService,
		ICloudinaryService cloudinaryService,
		AppDbContext context)
	{
		_gameDataService = gameDataService;
		_cloudinaryService = cloudinaryService;
		_context = context;
	}

	/// <summary>
	/// Same admin rule as the rest of the API: the DB user must have Role == "Admin".
	/// Returns null when allowed, otherwise the error result to return.
	/// </summary>
	private async Task<IActionResult?> RequireAdminAsync()
	{
		var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		var isAdmin = await _context.Users
			.AsNoTracking()
			.AnyAsync(x =>
				x.Auth0Id == auth0Id &&
				x.IsActive &&
				!x.IsDeleted &&
				x.Role == "Admin");

		return isAdmin ? null : Forbid();
	}

	[HttpGet("clubs")]
	public async Task<ActionResult<List<GameClubDto>>> GetAll()
	{
		var clubs = await _gameDataService.GetAllAsync();
		return Ok(clubs);
	}

	[HttpGet("clubs/{gameClubId:guid}")]
	public async Task<ActionResult<GameClubDto>> GetClubById(Guid gameClubId)
	{
		var club = await _gameDataService.GetByIdAsync(gameClubId);

		if (club is null)
			return NotFound();

		return Ok(club);
	}

	[Authorize]
	[HttpPost("clubs/import")]
	public async Task<IActionResult> Import(ImportGameClubsDto dto)
	{
		if (await RequireAdminAsync() is { } denied)
			return denied;

		var result = await _gameDataService.ImportAsync(dto);
		return Ok(result);
	}

	[Authorize]
	[HttpPost("clubs/sync-cmtracker")]
	public async Task<IActionResult> SyncCmTracker()
	{
		if (await RequireAdminAsync() is { } denied)
			return denied;

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

	[Authorize]
	[HttpPost("players/import-fc26-csv")]
	public async Task<IActionResult> ImportPlayersFromFc26Csv(
	IFormFile file)
	{
		if (await RequireAdminAsync() is { } denied)
			return denied;

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

	[Authorize]
	[HttpPost("{gameClubId:guid}/logo")]
	public async Task<IActionResult> UploadLogo(Guid gameClubId, IFormFile file)
	{
		if (await RequireAdminAsync() is { } denied)
			return denied;

		var result = await _gameDataService.UploadLogoAsync(gameClubId, file);

		if (result is null)
			return NotFound("Club not found.");

		return Ok(result);
	}
}
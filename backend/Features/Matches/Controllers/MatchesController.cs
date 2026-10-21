using backend.Data;
using backend.Features.Matches.Dtos;
using backend.Features.Matches.Services;
using backend.Features.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/matches")]
public class MatchesController : ControllerBase
{
	private readonly IMatchService _matchService;
	private readonly IAdminAccessService _adminAccess;
	private readonly AppDbContext _db;

	public MatchesController(
		IMatchService matchService,
		IAdminAccessService adminAccess,
		AppDbContext db)
	{
		_matchService = matchService;
		_adminAccess = adminAccess;
		_db = db;
	}

	[HttpGet("weeks")]
	public async Task<ActionResult<List<MatchWeekDto>>> GetWeeks([FromQuery] Guid seasonId)
	{
		var weeks = await _matchService.GetWeeksAsync(seasonId);
		return Ok(weeks);
	}

	[HttpGet("week/{weekNumber:int}")]
	public async Task<ActionResult<List<MatchDto>>> GetByWeek(
		int weekNumber,
		[FromQuery] Guid seasonId,
		[FromQuery] string? timeZone)
	{
		var matches = await _matchService.GetByWeekAsync(seasonId, weekNumber, timeZone);
		return Ok(matches);
	}

	[HttpGet("{matchId:guid}")]
	public async Task<ActionResult<MatchDto>> GetById(
		Guid matchId,
		[FromQuery] string? timeZone)
	{
		var match = await _matchService.GetByIdAsync(matchId, timeZone);

		if (match is null)
			return NotFound();

		return Ok(match);
	}

	[Authorize]
	[HttpPost("{matchId:guid}/result")]
	public async Task<ActionResult<MatchResultDto>> UploadResult(
		Guid matchId,
		[FromBody] UploadMatchResultDto dto)
	{
		var auth0Id = GetAuth0Id();

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized(new { message = "User token is missing Auth0 id." });

		try
		{
			await EnsureCanSubmitResultAsync(matchId, auth0Id);

			if (dto.HomeScore < 0 || dto.AwayScore < 0)
				return BadRequest(new { message = "Scores cannot be negative." });

			var result = await _matchService.UploadResultAsync(matchId, dto);

			if (result is null)
				return NotFound();

			// matchResult is published once inside MatchService.UploadResultAsync.
			return Ok(result);
		}
		catch (UnauthorizedAccessException ex)
		{
			return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
		}
	}

	[Authorize]
	[HttpPost("{matchId:guid}/stats-upload")]
	[Consumes("multipart/form-data")]
	public async Task<ActionResult<MatchStatsUploadResultDto>> UploadStatsImage(
		Guid matchId,
		[FromForm] UploadMatchStatsImageDto dto)
	{
		var auth0Id = GetAuth0Id();

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized(new { message = "User token is missing Auth0 id." });

		try
		{
			await EnsureCanSubmitResultAsync(matchId, auth0Id);

			var result = await _matchService.UploadStatsImageAsync(matchId, dto);

			if (result is null)
				return NotFound();

			return Ok(result);
		}
		catch (UnauthorizedAccessException ex)
		{
			return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
		}
	}

	[Authorize]
	[HttpPost("generate-schedule/{seasonId:guid}")]
	public async Task<IActionResult> GenerateSchedule(Guid seasonId)
	{
		var auth0Id = GetAuth0Id();

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized(new { message = "User token is missing Auth0 id." });

		try
		{
			await _adminAccess.EnsureAdminAsync(auth0Id);

			var generated = await _matchService.GenerateScheduleAsync(seasonId);

			if (!generated)
				return BadRequest(new { message = "Could not generate schedule." });

			return Ok();
		}
		catch (UnauthorizedAccessException ex)
		{
			return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
		}
	}

	[HttpGet("{matchId:guid}/manual-stats")]
	public async Task<IActionResult> GetManualStats(Guid matchId)
	{
		var result = await _matchService.GetManualStatsAsync(matchId);

		if (result is null)
		{
			return NotFound(new
			{
				message = "Match not found."
			});
		}

		return Ok(result);
	}

	[HttpGet("{matchId:guid}/player-options")]
	public async Task<IActionResult> GetPlayerOptions(
	Guid matchId)
	{
		var result = await _matchService
			.GetMatchPlayerOptionsAsync(matchId);

		if (result is null)
		{
			return NotFound(new
			{
				message = "Match not found."
			});
		}

		return Ok(result);
	}

	[Authorize]
	[HttpPost("{matchId:guid}/manual-stats")]
	public async Task<IActionResult> SaveManualStats(
	Guid matchId,
	[FromBody] SaveManualMatchStatsDto dto)
	{
		var auth0Id = GetAuth0Id();

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized(new { message = "User token is missing Auth0 id." });

		try
		{
			await EnsureCanSubmitResultAsync(matchId, auth0Id);

			var result = await _matchService.SaveManualStatsAsync(
				matchId,
				dto);

			if (!result)
				return NotFound(new
				{
					message = "Match not found."
				});

			await _matchService.UploadResultAsync(matchId, new UploadMatchResultDto
			{
				HomeScore = dto.HomeGoals,
				AwayScore = dto.AwayGoals
			});

			// UploadResultAsync publishes matchResult; no extra publish here.

			return Ok(new
			{
				message = "Match statistics saved successfully.",
				matchId
			});
		}
		catch (UnauthorizedAccessException ex)
		{
			return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
		}
	}

	private async Task EnsureCanSubmitResultAsync(Guid matchId, string auth0Id)
	{
		var user = await _db.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				x.IsActive &&
				!x.IsDeleted)
			?? throw new UnauthorizedAccessException("User not found.");

		var match = await _db.Matches
			.AsNoTracking()
			.Include(x => x.Tournament)
				.ThenInclude(x => x.Season)
			.FirstOrDefaultAsync(x => x.MatchId == matchId && !x.IsDeleted)
			?? throw new UnauthorizedAccessException("Match not found.");

		if (match.Tournament.Season is { IsActive: false })
			throw new UnauthorizedAccessException("This season is closed.");

		if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
			return;

		if (user.UserId != match.HomeUserId && user.UserId != match.AwayUserId)
			throw new UnauthorizedAccessException("Only the match participants or an admin can submit the result.");
	}

	private string? GetAuth0Id()
	{
		return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
			?? User.FindFirst("sub")?.Value
			?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
	}
}

using backend.Data;
using backend.Features.Stats.Dtos;
using backend.Features.Stats.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/match-stats")]
public class MatchStatsController : ControllerBase
{
	private readonly IMatchStatsOcrService _ocrService;
	private readonly IMatchStatsService _matchStatsService;
	private readonly AppDbContext _db;

	public MatchStatsController(
		IMatchStatsOcrService ocrService,
		IMatchStatsService matchStatsService,
		AppDbContext db)
	{
		_ocrService = ocrService;
		_matchStatsService = matchStatsService;
		_db = db;
	}

	[HttpPost("ocr-summary-preview")]
	[Consumes("multipart/form-data")]
	public async Task<IActionResult> OcrSummaryPreview(
		[FromForm] IFormFile image)
	{
		var result = await _ocrService
			.PreviewMatchSummaryAsync(image);

		return Ok(result);
	}

	[HttpPost("ocr-team-performance-preview")]
	[Consumes("multipart/form-data")]
	public async Task<IActionResult> OcrTeamPerformancePreview(
		[FromForm] IFormFile image)
	{
		var result = await _ocrService
			.PreviewTeamPerformanceAsync(image);

		return Ok(result);
	}

	[HttpPost("confirm-summary")]
	public async Task<IActionResult> ConfirmSummary(
		[FromBody] ConfirmMatchSummaryDto dto)
	{
		var denied = await EnsureCanSubmitResultAsync(dto.MatchId);
		if (denied is not null)
			return denied;

		await _matchStatsService
			.SaveMatchSummaryAsync(dto);

		return Ok();
	}

	[HttpPost("confirm-team-performance")]
	public async Task<IActionResult> ConfirmTeamPerformance(
		[FromBody] ConfirmTeamPerformanceDto dto)
	{
		var denied = await EnsureCanSubmitResultAsync(dto.MatchId);
		if (denied is not null)
			return denied;

		await _matchStatsService
			.SaveTeamPerformanceAsync(dto);

		return Ok();
	}

	/// <summary>Only the two match participants or an admin may write a match's result or stats.</summary>
	private async Task<IActionResult?> EnsureCanSubmitResultAsync(Guid matchId)
	{
		var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized(new { message = "User token is missing Auth0 id." });

		var user = await _db.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Auth0Id == auth0Id && x.IsActive && !x.IsDeleted);

		if (user is null)
			return StatusCode(StatusCodes.Status403Forbidden, new { message = "User not found." });

		var match = await _db.Matches
			.AsNoTracking()
			.Include(x => x.Tournament)
				.ThenInclude(x => x.Season)
			.FirstOrDefaultAsync(x => x.MatchId == matchId && !x.IsDeleted);

		if (match is null)
			return NotFound(new { message = "Match not found." });

		if (match.Tournament.Season is { IsActive: false })
			return StatusCode(StatusCodes.Status403Forbidden, new { message = "This season is closed." });

		var isAdmin = string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase);

		if (!isAdmin && user.UserId != match.HomeUserId && user.UserId != match.AwayUserId)
			return StatusCode(StatusCodes.Status403Forbidden,
				new { message = "Only the match participants or an admin can submit the result." });

		return null;
	}
}
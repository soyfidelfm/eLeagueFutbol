using backend.Features.Stats.Dtos;
using backend.Features.Stats.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/match-stats")]
public class MatchStatsController : ControllerBase
{
	private readonly IMatchStatsOcrService _ocrService;
	private readonly IMatchStatsService _matchStatsService;

	public MatchStatsController(
		IMatchStatsOcrService ocrService,
		IMatchStatsService matchStatsService)
	{
		_ocrService = ocrService;
		_matchStatsService = matchStatsService;
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
		await _matchStatsService
			.SaveMatchSummaryAsync(dto);

		return Ok();
	}

	[HttpPost("confirm-team-performance")]
	public async Task<IActionResult> ConfirmTeamPerformance(
		[FromBody] ConfirmTeamPerformanceDto dto)
	{
		await _matchStatsService
			.SaveTeamPerformanceAsync(dto);

		return Ok();
	}
}
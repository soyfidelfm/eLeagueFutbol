using backend.Application.Stats.DTOs;
using backend.Data;
using backend.Entities.Stats;
using backend.Features.Stats.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/match-stats")]
public class MatchStatsController : ControllerBase
{
	private readonly AppDbContext _context;
	private readonly IMatchStatsOcrService _ocrService;

	public MatchStatsController(
		AppDbContext context,
		IMatchStatsOcrService ocrService)
	{
		_context = context;
		_ocrService = ocrService;
	}

	[HttpPost("ocr-preview")]
	public async Task<IActionResult> OcrPreview(IFormFile image)
	{
		var result = await _ocrService.PreviewPlayerStatsAsync(image);
		return Ok(result);
	}

	[HttpPost("confirm-player")]
	public async Task<IActionResult> ConfirmPlayerStats([FromBody] ConfirmMatchPlayerStatsDto dto)
	{
		var entity = new MatchPlayerStats
		{
			MatchPlayerStatsId = Guid.NewGuid(),
			MatchId = dto.MatchId,
			UserId = dto.UserId,
			TeamId = dto.TeamId,

			Position = dto.Position,
			PlayerName = dto.PlayerName,
			Rating = dto.Rating,

			Goals = dto.Goals,
			Assists = dto.Assists,
			Shots = dto.Shots,
			ShotAccuracy = dto.ShotAccuracy,

			Passes = dto.Passes,
			PassAccuracy = dto.PassAccuracy,

			Dribbles = dto.Dribbles,
			DribbleSuccessRate = dto.DribbleSuccessRate,

			Tackles = dto.Tackles,
			TackleSuccessRate = dto.TackleSuccessRate,

			Offsides = dto.Offsides,
			FoulsCommitted = dto.FoulsCommitted,

			PossessionWon = dto.PossessionWon,
			PossessionLost = dto.PossessionLost,

			MinutesPlayed = dto.MinutesPlayed,
			DistanceCoveredKm = dto.DistanceCoveredKm,
			DistanceSprintedKm = dto.DistanceSprintedKm,

			YellowCards = dto.YellowCards,
			RedCards = dto.RedCards,

			IsPlayerOfTheMatch = dto.IsPlayerOfTheMatch
		};

		_context.MatchPlayerStats.Add(entity);
		await _context.SaveChangesAsync();

		return Ok(entity.MatchPlayerStatsId);
	}

	[HttpPost("ocr-match-preview")]
	public async Task<IActionResult> OcrMatchPreview(
	[FromForm] List<IFormFile> images)
	{
		var result =
			await _ocrService.PreviewMatchScreenshotsAsync(images);

		return Ok(result);
	}
}
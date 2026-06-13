using backend.Features.Matches.Dtos;
using backend.Features.Matches.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/matches")]
public class MatchesController : ControllerBase
{
	private readonly IMatchService _matchService;

	public MatchesController(IMatchService matchService)
	{
		_matchService = matchService;
	}

	[HttpGet("weeks")]
	public async Task<ActionResult<List<MatchWeekDto>>> GetWeeks(
		[FromQuery] Guid seasonId
	)
	{
		var weeks = await _matchService.GetWeeksAsync(seasonId);

		return Ok(weeks);
	}

	[HttpGet("week/{weekNumber:int}")]
	public async Task<ActionResult<List<MatchDto>>> GetByWeek(
		int weekNumber,
		[FromQuery] Guid seasonId,
		[FromQuery] string? timeZone
	)
	{
		var matches = await _matchService.GetByWeekAsync(
			seasonId,
			weekNumber,
			timeZone
		);

		return Ok(matches);
	}

	[HttpGet("{matchId:guid}")]
	public async Task<ActionResult<MatchDto>> GetById(
		Guid matchId,
		[FromQuery] string? timeZone
	)
	{
		var match = await _matchService.GetByIdAsync(
			matchId,
			timeZone
		);

		if (match is null)
			return NotFound();

		return Ok(match);
	}

	[HttpPost("{matchId:guid}/result")]
	public async Task<ActionResult<MatchResultDto>> UploadResult(
		Guid matchId,
		[FromBody] UploadMatchResultDto dto
	)
	{
		var result = await _matchService.UploadResultAsync(
			matchId,
			dto
		);

		if (result is null)
			return NotFound();

		return Ok(result);
	}

	[HttpPost("generate-schedule/{seasonId:guid}")]
	public async Task<IActionResult> GenerateSchedule(Guid seasonId)
	{
		var generated = await _matchService.GenerateScheduleAsync(seasonId);

		if (!generated)
			return BadRequest("Could not generate schedule.");

		return Ok();
	}
}
namespace backend.Features.Seasons.Controllers;

using backend.Features.Seasons.Dtos;
using backend.Features.Seasons.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/seasons")]
public class SeasonsController : ControllerBase
{
	private readonly ISeasonService _seasonService;

	public SeasonsController(ISeasonService seasonService)
	{
		_seasonService = seasonService;
	}

	[HttpGet]
	public async Task<ActionResult<List<SeasonDto>>> GetAll(
		CancellationToken ct)
	{
		var result = await _seasonService.GetAllAsync(ct);

		return Ok(result);
	}

	[HttpGet("{seasonId:guid}")]
	public async Task<ActionResult<SeasonDto>> GetById(
		Guid seasonId,
		CancellationToken ct)
	{
		var result = await _seasonService.GetByIdAsync(seasonId, ct);

		if (result is null)
			return NotFound();

		return Ok(result);
	}

	[HttpPost]
	public async Task<ActionResult<SeasonDto>> Create(
		[FromBody] CreateSeasonDto dto,
		CancellationToken ct)
	{
		var result = await _seasonService.CreateAsync(dto, ct);

		return CreatedAtAction(
			nameof(GetById),
			new { seasonId = result.SeasonId },
			result);
	}

	[HttpPut("{seasonId:guid}")]
	public async Task<ActionResult<SeasonDto>> Update(
		Guid seasonId,
		[FromBody] UpdateSeasonDto dto,
		CancellationToken ct)
	{
		var result = await _seasonService.UpdateAsync(seasonId, dto, ct);

		if (!result)
			return NotFound();

		return NoContent();
	}

	[HttpDelete("{seasonId:guid}")]
	public async Task<IActionResult> Delete(
		Guid seasonId,
		CancellationToken ct)
	{
		var deleted = await _seasonService.DeleteAsync(seasonId, ct);

		if (!deleted)
			return NotFound();

		return NoContent();
	}

	[HttpPost("{seasonId:guid}/register")]
	public async Task<ActionResult<SeasonRegistrationDto>> Register(
		Guid seasonId,
		[FromBody] RegisterSeasonDto dto,
		CancellationToken ct)
	{
		try
		{
			var result = await _seasonService.RegisterAsync(
				seasonId,
				dto,
				ct);

			return Ok(result);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
		catch (KeyNotFoundException ex)
		{
			return NotFound(new { message = ex.Message });
		}
	}

	[HttpGet("{seasonId:guid}/is-registered")]
	public async Task<ActionResult<bool>> IsRegistered(
		Guid seasonId,
		CancellationToken ct)
	{
		var result = await _seasonService.IsUserRegisteredAsync(
			seasonId,
			ct);

		return Ok(result);
	}
}
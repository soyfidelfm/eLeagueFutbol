using backend.Features.Seasons.Dtos;
using backend.Features.Seasons.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Features.Seasons.Controllers;

[Authorize]
[ApiController]
[Route("api/seasons")]
public class SeasonsController : ControllerBase
{
	private readonly ISeasonService _service;

	public SeasonsController(ISeasonService service)
	{
		_service = service;
	}

	[HttpGet]
	public async Task<ActionResult<IReadOnlyList<SeasonDto>>> GetAll(CancellationToken ct)
	{
		var seasons = await _service.GetAllAsync(ct);
		return Ok(seasons);
	}

	[HttpGet("{id:guid}")]
	public async Task<ActionResult<SeasonDto>> GetById(Guid id, CancellationToken ct)
	{
		var season = await _service.GetByIdAsync(id, ct);

		if (season is null)
			return NotFound();

		return Ok(season);
	}

	[HttpPost]
	public async Task<ActionResult<SeasonDto>> Create(
		[FromBody] CreateSeasonDto dto,
		CancellationToken ct)
	{
		var season = await _service.CreateAsync(dto, ct);

		return CreatedAtAction(
			nameof(GetById),
			new { id = season.SeasonId },
			season
		);
	}

	[HttpPut("{id:guid}")]
	public async Task<IActionResult> Update(
		Guid id,
		[FromBody] UpdateSeasonDto dto,
		CancellationToken ct)
	{
		var updated = await _service.UpdateAsync(id, dto, ct);

		if (!updated)
			return NotFound();

		return NoContent();
	}

	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
	{
		var deleted = await _service.DeleteAsync(id, ct);

		if (!deleted)
			return NotFound();

		return NoContent();
	}
}
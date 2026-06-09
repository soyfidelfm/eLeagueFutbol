namespace backend.Features.Seasons.Controllers;

using backend.Features.Seasons.Dtos;
using backend.Features.Seasons.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

	[Authorize]
	[HttpPost("{seasonId:guid}/register")]
	public async Task<ActionResult<SeasonRegistrationDto>> Register(
		Guid seasonId,
		[FromBody] RegisterSeasonDto dto,
		CancellationToken ct)
	{
		var auth0Id = GetAuth0Id();

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized(new { message = "User token is missing Auth0 id." });

		try
		{
			var result = await _seasonService.RegisterAsync(
				seasonId,
				dto,
				auth0Id,
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
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[Authorize]
	[HttpGet("{seasonId:guid}/is-registered")]
	public async Task<ActionResult<bool>> IsRegistered(
		Guid seasonId,
		CancellationToken ct)
	{
		var auth0Id = GetAuth0Id();

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized(false);

		var result = await _seasonService.IsUserRegisteredAsync(
			seasonId,
			auth0Id,
			ct);

		return Ok(result);
	}

	private string? GetAuth0Id()
	{
		return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
			?? User.FindFirst("sub")?.Value
			?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
	}
}
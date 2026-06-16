using System.Security.Claims;
using backend.Features.Draft.Dtos;
using backend.Features.Draft.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Features.Draft.Controllers;

[ApiController]
[Authorize]
[Route("api/draft")]
public class DraftController : ControllerBase
{
	private readonly IDraftService _draftService;

	public DraftController(IDraftService draftService)
	{
		_draftService = draftService;
	}

	[HttpGet("center")]
	public async Task<ActionResult<DraftCenterDto>> GetCenter([FromQuery] Guid seasonId)
	{
		var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		var result = await _draftService.GetCenterAsync(seasonId, auth0Id);

		if (result is null)
			return NotFound();

		return Ok(result);
	}

	[HttpGet("players")]
	public async Task<ActionResult<List<DraftPlayerDto>>> GetAvailablePlayers(
		[FromQuery] Guid seasonId,
		[FromQuery] string? search,
		[FromQuery] string? position,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 30)
	{
		var result = await _draftService.GetAvailablePlayersAsync(
			seasonId,
			search,
			position,
			page,
			pageSize);

		return Ok(result);
	}

	[HttpGet("picks")]
	public async Task<ActionResult<List<DraftPickDto>>> GetPicks([FromQuery] Guid seasonId)
	{
		var result = await _draftService.GetPicksAsync(seasonId);

		return Ok(result);
	}

	[HttpPost("picks")]
	public async Task<ActionResult<DraftPickDto>> MakePick(
		[FromQuery] Guid seasonId,
		[FromBody] MakeDraftPickDto dto)
	{
		var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		try
		{
			var result = await _draftService.MakePickAsync(
				seasonId,
				auth0Id,
				dto);

			return Ok(result);
		}
		catch (Exception ex)
		{
			return BadRequest(new
			{
				message = ex.Message
			});
		}
	}
}
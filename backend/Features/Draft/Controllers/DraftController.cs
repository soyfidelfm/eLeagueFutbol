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
	private readonly IDraftPickTradeService _draftPickTradeService;

	public DraftController(
		IDraftService draftService,
		IDraftPickTradeService draftPickTradeService)
	{
		_draftService = draftService;
		_draftPickTradeService = draftPickTradeService;
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

	[HttpPost("generate-order")]
	public async Task<ActionResult<DraftCenterDto>> GenerateDraftOrder(
		[FromQuery] Guid seasonId,
		[FromBody] StartDraftDto dto)
	{
		var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		try
		{
			var result = await _draftService.GenerateDraftOrderAsync(
				seasonId,
				auth0Id,
				dto);

			return Ok(result);
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpPost("start")]
	public async Task<ActionResult<DraftCenterDto>> StartDraft([FromQuery] Guid seasonId)
	{
		var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		try
		{
			var result = await _draftService.StartDraftAsync(seasonId, auth0Id);

			return Ok(result);
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
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
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpGet("preview-order")]
	public async Task<ActionResult<DraftLotteryPreviewDto>> PreviewDraftOrder(
		[FromQuery] Guid seasonId,
		[FromQuery] int drawNumber)
	{
		var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		try
		{
			var result = await _draftService.PreviewDraftOrderAsync(
				seasonId,
				drawNumber,
				auth0Id);

			return Ok(result);
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpGet("tradable-picks")]
	public async Task<ActionResult<List<DraftPickAssetDto>>> GetMyTradablePicks(
		[FromQuery] Guid seasonId)
	{
		var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		try
		{
			var result = await _draftPickTradeService.GetMyTradablePicksAsync(
				seasonId,
				auth0Id);

			return Ok(result);
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpPost("pick-trades")]
	public async Task<ActionResult<DraftPickTradeDto>> ProposePickTrade(
		[FromQuery] Guid seasonId,
		[FromBody] ProposeDraftPickTradeDto dto)
	{
		var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		try
		{
			var result = await _draftPickTradeService.ProposeTradeAsync(
				seasonId,
				auth0Id,
				dto);

			return Ok(result);
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpPost("pick-trades/{tradeId:guid}/accept")]
	public async Task<ActionResult<DraftPickTradeDto>> AcceptPickTrade(
		[FromQuery] Guid seasonId,
		[FromRoute] Guid tradeId)
	{
		var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		try
		{
			var result = await _draftPickTradeService.AcceptTradeAsync(
				seasonId,
				auth0Id,
				tradeId);

			return Ok(result);
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpPost("pick-trades/{tradeId:guid}/reject")]
	public async Task<IActionResult> RejectPickTrade(
		[FromQuery] Guid seasonId,
		[FromRoute] Guid tradeId)
	{
		var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		try
		{
			await _draftPickTradeService.RejectTradeAsync(
				seasonId,
				auth0Id,
				tradeId);

			return Ok(new { message = "Trade rejected." });
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpPost("pick-trades/{tradeId:guid}/cancel")]
	public async Task<IActionResult> CancelPickTrade(
		[FromQuery] Guid seasonId,
		[FromRoute] Guid tradeId)
	{
		var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		try
		{
			await _draftPickTradeService.CancelTradeAsync(
				seasonId,
				auth0Id,
				tradeId);

			return Ok(new { message = "Trade cancelled." });
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}
}
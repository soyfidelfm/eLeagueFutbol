namespace backend.Features.Transfers.Controllers;

using System.Security.Claims;
using backend.Features.Transfers.Dtos;
using backend.Features.Transfers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("api/transfers")]
public class TransfersController : ControllerBase
{
	private readonly ITransferService _transfers;

	public TransfersController(ITransferService transfers)
	{
		_transfers = transfers;
	}

	[HttpGet("board")]
	public async Task<ActionResult<TransferBoardDto>> GetBoard([FromQuery] Guid seasonId)
	{
		try
		{
			return Ok(await _transfers.GetBoardAsync(GetAuth0Id(), seasonId));
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpPost("offers")]
	public async Task<ActionResult<TransferOfferDto>> CreateOffer(
		[FromQuery] Guid seasonId,
		[FromBody] CreateTransferOfferDto dto)
	{
		try
		{
			return Ok(await _transfers.CreateOfferAsync(GetAuth0Id(), seasonId, dto));
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpPost("offers/{transferRecordId:guid}/accept")]
	public async Task<ActionResult<TransferOfferDto>> Accept(Guid transferRecordId)
	{
		try
		{
			return Ok(await _transfers.AcceptOfferAsync(GetAuth0Id(), transferRecordId));
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpPost("offers/{transferRecordId:guid}/reject")]
	public async Task<ActionResult<TransferOfferDto>> Reject(Guid transferRecordId)
	{
		try
		{
			return Ok(await _transfers.RejectOfferAsync(GetAuth0Id(), transferRecordId));
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpPost("offers/{transferRecordId:guid}/cancel")]
	public async Task<IActionResult> Cancel(Guid transferRecordId)
	{
		try
		{
			await _transfers.CancelOfferAsync(GetAuth0Id(), transferRecordId);
			return NoContent();
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	private string GetAuth0Id()
	{
		return User.FindFirstValue(ClaimTypes.NameIdentifier)
			?? User.FindFirstValue("sub")
			?? throw new UnauthorizedAccessException("Auth0 user id not found.");
	}
}

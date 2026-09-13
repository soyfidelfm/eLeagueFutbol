namespace backend.Features.Teams.Controllers;

using System.Security.Claims;
using backend.Features.Teams.Dtos;
using backend.Features.Teams.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("api/teams")]
public class TeamsController : ControllerBase
{
	private readonly ITeamService _teams;

	public TeamsController(ITeamService teams)
	{
		_teams = teams;
	}

	[HttpGet]
	public async Task<ActionResult<List<TeamDto>>> GetAll()
	{
		try
		{
			return Ok(await _teams.GetAllAsync(GetAuth0Id()));
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpGet("mine")]
	public async Task<ActionResult<TeamDto?>> GetMine()
	{
		try
		{
			return Ok(await _teams.GetMineAsync(GetAuth0Id()));
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpPost]
	public async Task<ActionResult<TeamDto>> Create([FromBody] CreateTeamDto dto)
	{
		try
		{
			return Ok(await _teams.CreateAsync(GetAuth0Id(), dto));
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpPost("{teamId:guid}/members")]
	public async Task<ActionResult<TeamDto>> AddMember(Guid teamId, [FromBody] AddTeamMemberDto dto)
	{
		try
		{
			return Ok(await _teams.AddMemberAsync(GetAuth0Id(), teamId, dto));
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpDelete("{teamId:guid}/members/{userId:guid}")]
	public async Task<IActionResult> RemoveMember(Guid teamId, Guid userId)
	{
		try
		{
			await _teams.LeaveOrRemoveAsync(GetAuth0Id(), teamId, userId);
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

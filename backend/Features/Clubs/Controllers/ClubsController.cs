using backend.Features.Clubs.Dtos;
using backend.Features.Clubs.DTOs;
using backend.Features.Clubs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Features.Clubs.Controllers;

[ApiController]
[Route("api/clubs")]
[Authorize]
public class ClubsController : ControllerBase
{
	private readonly IClubService _clubService;

	public ClubsController(IClubService clubService)
	{
		_clubService = clubService;
	}

	[HttpGet("my")]
	public async Task<IActionResult> GetMyClub()
	{
		var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		var club = await _clubService.GetMyClubAsyncByAuth0Id(auth0Id);

		if (club is null)
			return NotFound("No club found for this user.");

		return Ok(club);
	}

	[HttpPut("my/logo")]
	public async Task<IActionResult> UpdateMyClubLogo(
		[FromBody] UpdateClubLogoDto request)
	{
		var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		var club = await _clubService.UpdateClubLogoAsync(
			auth0Id,
			request.LogoUrl);

		if (club is null)
			return NotFound("Club not found.");

		return Ok(club);
	}

	[HttpPut("my/lineup")]
	public async Task<IActionResult> UpdateMyClubLineup(
		[FromBody] UpdateMyClubLineupDto request)
	{
		var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		var club = await _clubService.UpdateMyClubLineupAsync(
			auth0Id,
			request);

		if (club is null)
			return BadRequest("Unable to update lineup.");

		return Ok(club);
	}
}
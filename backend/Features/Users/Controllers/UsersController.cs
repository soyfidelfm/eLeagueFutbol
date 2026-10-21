using backend.Features.Users.Dtos;
using backend.Features.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Features.Users.Controllers;

[Authorize]
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
	private readonly IUserService _service;

	public UsersController(IUserService service)
	{
		_service = service;
	}

	[HttpGet]
	public async Task<IActionResult> GetAll()
	{
		return Ok(await _service.GetAllAsync());
	}

	[HttpGet("{userId:guid}")]
	public async Task<IActionResult> GetById(Guid userId)
	{
		var user = await _service.GetByIdAsync(userId);

		if (user == null)
			return NotFound();

		return Ok(user);
	}

	[HttpPost("sync")]
	[Authorize]
	public async Task<ActionResult<UserDto>> Sync([FromBody] SyncUserDto dto)
	{
		var user = await _service.SyncAsync(dto, User);

		if (user == null)
			return Unauthorized();

		return Ok(user);
	}

	[HttpPut("{userId:guid}")]
	public async Task<IActionResult> Update(Guid userId, UpdateUserDto dto)
	{
		var user = await _service.UpdateAsync(userId, dto);

		if (user == null)
			return NotFound();

		return Ok(user);
	}

	[Authorize]
	[HttpGet("me")]
	public async Task<ActionResult<MeUserDto>> GetMe()
	{
		var me = await _service.GetMeAsync(User);

		if (me == null)
			return NotFound("User not found.");

		return Ok(me);
	}

	[HttpGet("me/settings")]
	public async Task<ActionResult<UserSettingsDto>> GetMySettings()
	{
		var settings = await _service.GetSettingsAsync(User);

		if (settings == null)
			return NotFound(new { message = "User not found." });

		return Ok(settings);
	}

	[HttpPut("me/settings")]
	public async Task<ActionResult<UserSettingsDto>> UpdateMySettings([FromBody] UserSettingsDto dto)
	{
		try
		{
			var settings = await _service.UpdateSettingsAsync(User, dto);

			if (settings == null)
				return NotFound(new { message = "User not found." });

			return Ok(settings);
		}
		catch (ArgumentException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpGet("search")]
	public async Task<ActionResult<List<UserDto>>> Search([FromQuery] string term)
	{
		var users = await _service.SearchAsync(term, User);
		return Ok(users);
	}
}
using backend.Features.Users.Dtos;
using backend.Features.Users.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Features.Users.Controllers;

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
	public async Task<IActionResult> Sync(SyncUserDto dto)
	{
		var user = await _service.SyncAsync(dto);

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
}
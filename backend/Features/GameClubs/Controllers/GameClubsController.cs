using backend.Features.GameClubs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Features.GameClubs.Controllers;

[Authorize]
[ApiController]
[Route("api/gameclubs")]
public class GameClubsController : ControllerBase
{
	private readonly IGameClubService _service;

	public GameClubsController(IGameClubService service)
	{
		_service = service;
	}

	[HttpGet]
	public async Task<IActionResult> GetAll()
	{
		var clubs = await _service.GetAllAsync();

		return Ok(clubs);
	}
}
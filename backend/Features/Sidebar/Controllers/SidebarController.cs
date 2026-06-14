using backend.Features.Sidebar.Dtos;
using backend.Features.Sidebar.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Features.Sidebar.Controllers;

[ApiController]
[Route("api/sidebar")]
public class SidebarController : ControllerBase
{
	private readonly ISidebarService _sidebarService;

	public SidebarController(ISidebarService sidebarService)
	{
		_sidebarService = sidebarService;
	}

	[HttpGet]
	public async Task<ActionResult<SidebarDto>> Get()
	{
		var result = await _sidebarService.GetAsync();
		return Ok(result);
	}
}
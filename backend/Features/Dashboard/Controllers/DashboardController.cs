using System.Security.Claims;
using backend.Features.Dashboard.Dtos;
using backend.Features.Dashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Features.Dashboard.Controllers;

[Authorize]
[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
	private readonly IDashboardService _dashboardService;

	public DashboardController(IDashboardService dashboardService)
	{
		_dashboardService = dashboardService;
	}

	[HttpGet]
	public async Task<ActionResult<DashboardDto>> Get(
		[FromQuery] string timeZone = "UTC",
		CancellationToken ct = default)
	{
		var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		var dashboard = await _dashboardService.GetAsync(auth0Id, timeZone, ct);

		return Ok(dashboard);
	}
}
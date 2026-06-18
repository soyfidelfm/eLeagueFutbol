namespace backend.Features.Notifications.Controllers;

using System.Security.Claims;
using backend.Features.Notifications.Dtos;
using backend.Features.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
	private readonly INotificationService _notificationService;

	public NotificationsController(INotificationService notificationService)
	{
		_notificationService = notificationService;
	}

	[HttpPost("token")]
	public async Task<IActionResult> SaveToken(
		[FromBody] SaveNotificationTokenDto dto)
	{
		var auth0Id =
			User.FindFirstValue(ClaimTypes.NameIdentifier) ??
			User.FindFirstValue("sub") ??
			string.Empty;

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		await _notificationService.SaveTokenAsync(auth0Id, dto);

		return Ok();
	}
}
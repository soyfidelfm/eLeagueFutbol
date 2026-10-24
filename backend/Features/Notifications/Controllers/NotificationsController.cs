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
	private readonly INotificationInboxService _notificationInboxService;

	public NotificationsController(
		INotificationService notificationService,
		INotificationInboxService notificationInboxService)
	{
		_notificationService = notificationService;
		_notificationInboxService = notificationInboxService;
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

	[HttpGet]
	public async Task<ActionResult<List<NotificationDto>>> GetMyNotifications(
		[FromQuery] bool unreadOnly = false,
		[FromQuery] DateTime? before = null,
		[FromQuery] int take = 50)
	{
		var auth0Id =
			User.FindFirstValue(ClaimTypes.NameIdentifier) ??
			User.FindFirstValue("sub") ??
			string.Empty;

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		var notifications =
			await _notificationInboxService.GetMyNotificationsAsync(
				auth0Id,
				unreadOnly,
				before,
				take);

		return Ok(notifications);
	}

	[HttpGet("unread-count")]
	public async Task<ActionResult<UnreadNotificationCountDto>> GetUnreadCount()
	{
		var auth0Id =
			User.FindFirstValue(ClaimTypes.NameIdentifier) ??
			User.FindFirstValue("sub") ??
			string.Empty;

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		var result =
			await _notificationInboxService.GetUnreadCountAsync(auth0Id);

		return Ok(result);
	}

	[HttpPut("{notificationId:guid}/read")]
	public async Task<IActionResult> MarkAsRead(Guid notificationId)
	{
		var auth0Id =
			User.FindFirstValue(ClaimTypes.NameIdentifier) ??
			User.FindFirstValue("sub") ??
			string.Empty;

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		await _notificationInboxService.MarkAsReadAsync(
			auth0Id,
			notificationId);

		return NoContent();
	}

	[HttpPut("read-all")]
	public async Task<IActionResult> MarkAllAsRead()
	{
		var auth0Id =
			User.FindFirstValue(ClaimTypes.NameIdentifier) ??
			User.FindFirstValue("sub") ??
			string.Empty;

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		await _notificationInboxService.MarkAllAsReadAsync(auth0Id);

		return NoContent();
	}
}
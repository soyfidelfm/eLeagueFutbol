namespace backend.Features.Notifications.Services;

using backend.Features.Notifications.Dtos;

public interface INotificationInboxService
{
	/// <summary>Newest first. <paramref name="before"/> is a CreatedAtUTC cursor (exclusive).</summary>
	Task<List<NotificationDto>> GetMyNotificationsAsync(
		string auth0Id,
		bool unreadOnly = false,
		DateTime? before = null,
		int take = 50);

	Task<UnreadNotificationCountDto> GetUnreadCountAsync(string auth0Id);

	Task MarkAsReadAsync(string auth0Id, Guid notificationId);

	Task MarkAllAsReadAsync(string auth0Id);
}
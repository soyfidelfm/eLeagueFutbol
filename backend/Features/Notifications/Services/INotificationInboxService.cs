namespace backend.Features.Notifications.Services;

using backend.Features.Notifications.Dtos;

public interface INotificationInboxService
{
	Task<List<NotificationDto>> GetMyNotificationsAsync(string auth0Id);

	Task<UnreadNotificationCountDto> GetUnreadCountAsync(string auth0Id);

	Task MarkAsReadAsync(string auth0Id, Guid notificationId);

	Task MarkAllAsReadAsync(string auth0Id);
}
namespace backend.Features.Notifications.Services;

using backend.Features.Notifications.Dtos;

public interface INotificationService
{
	Task SaveTokenAsync(string auth0Id, SaveNotificationTokenDto dto);

	Task SendToUserAsync(
		Guid userId,
		string title,
		string body,
		Dictionary<string, string>? data = null);
}
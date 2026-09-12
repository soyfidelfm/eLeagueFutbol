namespace backend.Features.Realtime;

using backend.Features.Messages.Dtos;
using backend.Features.Notifications.Dtos;

public interface IAppRealtimeService
{
	bool IsOnline(Guid userId);

	Task ReceiveMessageAsync(Guid userId, MessageDto message);

	Task MessageReadAsync(Guid userId, MessageReadDto dto);

	Task NotificationCreatedAsync(Guid userId, NotificationDto notification);

	Task PublishDraftEventAsync(Guid seasonId, string eventType);

	Task UserOnlineAsync(PresenceUserDto user);

	Task UserOfflineAsync(PresenceUserDto user);
}

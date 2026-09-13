namespace backend.Features.Realtime;

using backend.Features.Messages.Dtos;
using backend.Features.Notifications.Dtos;
using Microsoft.AspNetCore.SignalR;

public class AppRealtimeService : IAppRealtimeService
{
	private readonly IHubContext<AppHub> _hub;
	private readonly PresenceTracker _presence;

	public AppRealtimeService(IHubContext<AppHub> hub, PresenceTracker presence)
	{
		_hub = hub;
		_presence = presence;
	}

	public bool IsOnline(Guid userId) => _presence.IsOnline(userId);

	public Task ReceiveMessageAsync(Guid userId, MessageDto message)
	{
		return _hub.Clients.Group($"user:{userId}").SendAsync("ReceiveMessage", message);
	}

	public Task MessageReadAsync(Guid userId, MessageReadDto dto)
	{
		return _hub.Clients.Group($"user:{userId}").SendAsync("MessageRead", dto);
	}

	public Task NotificationCreatedAsync(Guid userId, NotificationDto notification)
	{
		return _hub.Clients.Group($"user:{userId}").SendAsync("NotificationCreated", notification);
	}

	public Task PublishDraftEventAsync(Guid seasonId, string eventType)
	{
		var payload = new DraftRealtimeEventDto
		{
			SeasonId = seasonId,
			EventType = eventType
		};

		return Task.WhenAll(
			_hub.Clients.Group($"season:{seasonId}").SendAsync("DraftEvent", payload),
			_hub.Clients.Group("online").SendAsync("DraftEvent", payload));
	}

	public Task UserOnlineAsync(PresenceUserDto user)
	{
		return _hub.Clients.Group("online").SendAsync("UserOnline", user);
	}

	public Task UserOfflineAsync(PresenceUserDto user)
	{
		return _hub.Clients.Group("online").SendAsync("UserOffline", user);
	}
}

using backend.Features.Realtime.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace backend.Features.Realtime.Services;

public class RealtimeService : IRealtimeService
{
	private readonly IHubContext<RealtimeHub> _hub;

	public RealtimeService(IHubContext<RealtimeHub> hub)
	{
		_hub = hub;
	}

	public async Task SendToUserAsync(
		Guid userId,
		string eventName,
		object payload)
	{
		await _hub.Clients
			.Group($"user-{userId}")
			.SendAsync(eventName, payload);
	}
}
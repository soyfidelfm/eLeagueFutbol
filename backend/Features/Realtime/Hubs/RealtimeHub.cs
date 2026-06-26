using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace backend.Features.Realtime.Hubs;

[Authorize]
public class RealtimeHub : Hub
{
	public override async Task OnConnectedAsync()
	{
		var userId = Context.UserIdentifier;

		if (!string.IsNullOrWhiteSpace(userId))
		{
			await Groups.AddToGroupAsync(
				Context.ConnectionId,
				$"user-{userId}");
		}

		await base.OnConnectedAsync();
	}
}
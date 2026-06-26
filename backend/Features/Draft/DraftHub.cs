using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace backend.Features.Draft.Hubs;

[Authorize]
public class DraftHub : Hub
{
	public async Task JoinSeasonDraft(string seasonId)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, $"draft-{seasonId}");
	}

	public async Task LeaveSeasonDraft(string seasonId)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"draft-{seasonId}");
	}
}
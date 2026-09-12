namespace backend.Features.Realtime;

using System.Security.Claims;
using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

[Authorize]
public class AppHub : Hub
{
	private readonly AppDbContext _context;
	private readonly PresenceTracker _presence;
	private readonly IAppRealtimeService _realtime;

	public AppHub(
		AppDbContext context,
		PresenceTracker presence,
		IAppRealtimeService realtime)
	{
		_context = context;
		_presence = presence;
		_realtime = realtime;
	}

	public override async Task OnConnectedAsync()
	{
		var user = await GetCurrentUserAsync();

		if (user is null)
		{
			Context.Abort();
			return;
		}

		Context.Items["UserId"] = user.UserId;

		var presence = new PresenceUserDto
		{
			UserId = user.UserId,
			DisplayName = user.DisplayName,
			AvatarUrl = user.AvatarUrl,
			LastSeenAtUTC = DateTime.UtcNow
		};

		var becameOnline = _presence.AddConnection(presence, Context.ConnectionId);

		await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{user.UserId}");
		await Groups.AddToGroupAsync(Context.ConnectionId, "online");

		user.LastLoginAtUTC = DateTime.UtcNow;
		await _context.SaveChangesAsync();

		await Clients.Caller.SendAsync("OnlineUsers", _presence.GetOnlineUsers());

		if (becameOnline)
			await _realtime.UserOnlineAsync(presence);

		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		if (Context.Items.TryGetValue("UserId", out var value) && value is Guid userId)
		{
			var becameOffline = _presence.RemoveConnection(userId, Context.ConnectionId);

			if (becameOffline)
			{
				var user = await _context.Users.FirstOrDefaultAsync(x =>
					x.UserId == userId && !x.IsDeleted);

				if (user is not null)
				{
					user.LastLoginAtUTC = DateTime.UtcNow;
					await _context.SaveChangesAsync();

					await _realtime.UserOfflineAsync(new PresenceUserDto
					{
						UserId = user.UserId,
						DisplayName = user.DisplayName,
						AvatarUrl = user.AvatarUrl,
						LastSeenAtUTC = user.LastLoginAtUTC
					});
				}
			}
		}

		await base.OnDisconnectedAsync(exception);
	}

	public async Task JoinSeason(Guid seasonId)
	{
		if (seasonId == Guid.Empty)
			return;

		await Groups.AddToGroupAsync(Context.ConnectionId, $"season:{seasonId}");
	}

	public async Task LeaveSeason(Guid seasonId)
	{
		if (seasonId == Guid.Empty)
			return;

		await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"season:{seasonId}");
	}

	public Task<IReadOnlyList<PresenceUserDto>> GetOnlineUsers()
	{
		return Task.FromResult(_presence.GetOnlineUsers());
	}

	private async Task<Entities.Identity.User?> GetCurrentUserAsync()
	{
		var auth0Id =
			Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
			Context.User?.FindFirstValue("sub");

		if (string.IsNullOrWhiteSpace(auth0Id))
			return null;

		return await _context.Users.FirstOrDefaultAsync(x =>
			x.Auth0Id == auth0Id &&
			!x.IsDeleted &&
			x.IsActive);
	}
}

namespace backend.Features.Notifications.Services;

using backend.Data;
using backend.Features.Notifications.Dtos;
using Microsoft.EntityFrameworkCore;

public class NotificationInboxService : INotificationInboxService
{
	private readonly AppDbContext _context;

	public NotificationInboxService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<List<NotificationDto>> GetMyNotificationsAsync(
		string auth0Id,
		bool unreadOnly = false,
		DateTime? before = null,
		int take = 50)
	{
		var me = await _context.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted);

		if (me is null)
			return [];

		take = Math.Clamp(take, 1, 100);

		var query = _context.Notifications
			.AsNoTracking()
			.Where(x =>
				x.UserId == me.UserId &&
				!x.IsDeleted);

		if (unreadOnly)
			query = query.Where(x => !x.IsRead);

		if (before.HasValue)
		{
			// Cursor is the CreatedAtUTC of the last item the client has; treat zone-less values as UTC.
			var cursor = before.Value.Kind == DateTimeKind.Unspecified
				? DateTime.SpecifyKind(before.Value, DateTimeKind.Utc)
				: before.Value.ToUniversalTime();
			query = query.Where(x => x.CreatedAtUTC < cursor);
		}

		return await query
			.OrderByDescending(x => x.CreatedAtUTC)
			.ThenByDescending(x => x.NotificationId)
			.Take(take)
			.Select(x => new NotificationDto
			{
				NotificationId = x.NotificationId,
				Title = x.Title,
				Message = x.Message,
				Type = x.Type,
				Url = x.Url,
				IsRead = x.IsRead,
				CreatedAtUTC = x.CreatedAtUTC
			})
			.ToListAsync();
	}

	public async Task<UnreadNotificationCountDto> GetUnreadCountAsync(string auth0Id)
	{
		var me = await _context.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted);

		if (me is null)
			return new UnreadNotificationCountDto();

		var count = await _context.Notifications
			.CountAsync(x =>
				x.UserId == me.UserId &&
				!x.IsDeleted &&
				!x.IsRead);

		return new UnreadNotificationCountDto
		{
			Count = count
		};
	}

	public async Task MarkAsReadAsync(string auth0Id, Guid notificationId)
	{
		var me = await _context.Users
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted);

		if (me is null)
			throw new Exception("User not found.");

		var notification = await _context.Notifications
			.FirstOrDefaultAsync(x =>
				x.NotificationId == notificationId &&
				x.UserId == me.UserId &&
				!x.IsDeleted);

		if (notification is null)
			return;

		if (notification.IsRead)
			return;

		notification.IsRead = true;
		notification.ReadAtUTC = DateTime.UtcNow;

		await _context.SaveChangesAsync();
	}

	public async Task MarkAllAsReadAsync(string auth0Id)
	{
		var me = await _context.Users
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted);

		if (me is null)
			throw new Exception("User not found.");

		var notifications = await _context.Notifications
			.Where(x =>
				x.UserId == me.UserId &&
				!x.IsDeleted &&
				!x.IsRead)
			.ToListAsync();

		if (notifications.Count == 0)
			return;

		var now = DateTime.UtcNow;

		foreach (var notification in notifications)
		{
			notification.IsRead = true;
			notification.ReadAtUTC = now;
		}

		await _context.SaveChangesAsync();
	}
}
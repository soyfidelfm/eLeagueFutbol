namespace backend.Features.Notifications.Services;

using backend.Data;
using backend.Entities.Notifications;
using backend.Features.Notifications.Dtos;
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;

public class NotificationService : INotificationService
{
	private readonly AppDbContext _context;

	public NotificationService(AppDbContext context)
	{
		_context = context;
	}

	public async Task SaveTokenAsync(string auth0Id, SaveNotificationTokenDto dto)
	{
		if (string.IsNullOrWhiteSpace(dto.Token))
			throw new Exception("FCM token is required.");

		var user = await _context.Users
			.FirstOrDefaultAsync(x =>
				!x.IsDeleted &&
				x.Auth0Id == auth0Id);

		if (user is null)
			return;

		var token = dto.Token.Trim();

		var deviceType = string.IsNullOrWhiteSpace(dto.DeviceType)
			? "web"
			: dto.DeviceType.Trim();

		var existingToken = await _context.NotificationTokens
			.FirstOrDefaultAsync(x =>
				x.FcmToken == token ||
				(x.UserId == user.UserId && x.DeviceType == deviceType));

		if (existingToken is not null)
		{
			existingToken.UserId = user.UserId;
			existingToken.FcmToken = token;
			existingToken.DeviceType = deviceType;
			existingToken.LastUsedAtUTC = DateTime.UtcNow;

			await _context.SaveChangesAsync();
			return;
		}

		var notificationToken = new NotificationToken
		{
			NotificationTokenId = Guid.NewGuid(),
			UserId = user.UserId,
			FcmToken = token,
			DeviceType = deviceType,
			LastUsedAtUTC = DateTime.UtcNow
		};

		_context.NotificationTokens.Add(notificationToken);

		await _context.SaveChangesAsync();
	}

	public async Task SendToUserAsync(
		Guid userId,
		string title,
		string body,
		Dictionary<string, string>? data = null)
	{
		var tokens = await _context.NotificationTokens
			.Where(x =>
				!x.IsDeleted &&
				x.UserId == userId &&
				!string.IsNullOrWhiteSpace(x.FcmToken))
			.ToListAsync();

		if (tokens.Count == 0)
			return;

		var firebase = FirebaseMessaging.DefaultInstance;

		if (firebase is null)
			return;

		var safeData = data ?? new Dictionary<string, string>();

		if (!safeData.ContainsKey("title"))
			safeData["title"] = title ?? string.Empty;

		if (!safeData.ContainsKey("body"))
			safeData["body"] = body ?? string.Empty;

		safeData.TryGetValue("url", out var url);

		foreach (var token in tokens)
		{
			try
			{
				var message = new Message
				{
					Token = token.FcmToken,

					Notification = new FirebaseAdmin.Messaging.Notification
					{
						Title = title ?? string.Empty,
						Body = body ?? string.Empty
					},

					Data = safeData,

					Webpush = new WebpushConfig
					{
						FcmOptions = string.IsNullOrWhiteSpace(url)
							? null
							: new WebpushFcmOptions { Link = url },
						Notification = new WebpushNotification
						{
							Title = title ?? string.Empty,
							Body = body ?? string.Empty,
							Icon = "/images/iOSicon.png"
						}
					}
				};

				await firebase.SendAsync(message);

				token.LastUsedAtUTC = DateTime.UtcNow;
			}
			catch (FirebaseMessagingException ex)
			{
				if (
					ex.MessagingErrorCode == MessagingErrorCode.Unregistered ||
					ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
				{
					_context.NotificationTokens.Remove(token);
				}
			}
		}

		await _context.SaveChangesAsync();
	}
}

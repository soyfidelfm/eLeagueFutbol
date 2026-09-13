namespace backend.Features.Messages.Services;

using backend.Data;
using backend.Entities.Messaging;
using backend.Entities.Notifications;
using backend.Features.Messages.Dtos;
using backend.Features.Notifications.Dtos;
using backend.Features.Notifications.Services;
using backend.Features.Realtime;
using Microsoft.EntityFrameworkCore;

public class MessageService : IMessageService
{
	private readonly AppDbContext _context;
	private readonly INotificationService _notificationService;
	private readonly IAppRealtimeService _realtime;

	public MessageService(
		AppDbContext context,
		INotificationService notificationService,
		IAppRealtimeService realtime)
	{
		_context = context;
		_notificationService = notificationService;
		_realtime = realtime;
	}

	public async Task<List<ConversationDto>> GetConversationsAsync(string auth0Id)
	{
		var me = await _context.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted);

		if (me is null)
			return [];

		var messages = await _context.Messages
			.AsNoTracking()
			.Include(x => x.SenderUser)
			.Include(x => x.ReceiverUser)
			.Where(x =>
				!x.IsDeleted &&
				(x.SenderUserId == me.UserId || x.ReceiverUserId == me.UserId))
			.OrderByDescending(x => x.SentAtUTC)
			.ToListAsync();

		var conversations = messages
			.GroupBy(x =>
				x.SenderUserId == me.UserId
					? x.ReceiverUserId
					: x.SenderUserId)
			.Select(group =>
			{
				var last = group.OrderByDescending(x => x.SentAtUTC).First();

				var otherUser = last.SenderUserId == me.UserId
					? last.ReceiverUser
					: last.SenderUser;

				return new ConversationDto
				{
					UserId = otherUser.UserId,
					DisplayName = otherUser.DisplayName,
					AvatarUrl = otherUser.AvatarUrl,
					LastMessage = last.Text,
					LastMessageAtUTC = last.SentAtUTC,
					UnreadCount = group.Count(x =>
						x.ReceiverUserId == me.UserId &&
						!x.IsRead)
				};
			})
			.OrderByDescending(x => x.LastMessageAtUTC)
			.ToList();

		return conversations;
	}

	public async Task<List<MessageDto>> GetConversationAsync(string auth0Id, Guid otherUserId)
	{
		var me = await _context.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted);

		if (me is null)
			return [];

		var messages = await _context.Messages
			.AsNoTracking()
			.Include(x => x.SenderUser)
			.Include(x => x.ReceiverUser)
			.Where(x =>
				!x.IsDeleted &&
				(
					(x.SenderUserId == me.UserId && x.ReceiverUserId == otherUserId) ||
					(x.SenderUserId == otherUserId && x.ReceiverUserId == me.UserId)
				))
			.OrderBy(x => x.SentAtUTC)
			.Select(x => new MessageDto
			{
				MessageId = x.MessageId,
				SenderUserId = x.SenderUserId,
				SenderName = x.SenderUser.DisplayName,
				ReceiverUserId = x.ReceiverUserId,
				ReceiverName = x.ReceiverUser.DisplayName,
				Text = x.Text,
				IsMine = x.SenderUserId == me.UserId,
				IsRead = x.IsRead,
				SentAtUTC = x.SentAtUTC
			})
			.ToListAsync();

		return messages;
	}

	public async Task<MessageDto> SendAsync(string auth0Id, SendMessageDto dto)
	{
		var sender = await _context.Users
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted);

		if (sender is null)
			throw new Exception("User not found.");

		var receiver = await _context.Users
			.FirstOrDefaultAsync(x =>
				x.UserId == dto.ReceiverUserId &&
				!x.IsDeleted);

		if (receiver is null)
			throw new Exception("Receiver user not found.");

		if (sender.UserId == receiver.UserId)
			throw new Exception("You cannot send a message to yourself.");

		var text = dto.Text.Trim();

		if (string.IsNullOrWhiteSpace(text))
			throw new Exception("Message text is required.");

		if (text.Length > 1000)
			throw new Exception("Message text cannot be longer than 1000 characters.");

		var now = DateTime.UtcNow;

		var message = new Message
		{
			MessageId = Guid.NewGuid(),
			SenderUserId = sender.UserId,
			ReceiverUserId = receiver.UserId,
			Text = text,
			IsRead = false,
			SentAtUTC = now
		};

		var preview = text.Length > 80
			? text[..80] + "..."
			: text;

		var notification = new Notification
		{
			NotificationId = Guid.NewGuid(),
			UserId = receiver.UserId,
			Title = "New message",
			Message = $"{sender.DisplayName}: {preview}",
			Type = "message",
			Url = $"/messages?userId={sender.UserId}",
			IsRead = false,
			CreatedAtUTC = now
		};

		_context.Messages.Add(message);
		_context.Notifications.Add(notification);

		await _context.SaveChangesAsync();

		var senderDto = new MessageDto
		{
			MessageId = message.MessageId,
			SenderUserId = sender.UserId,
			SenderName = sender.DisplayName,
			ReceiverUserId = receiver.UserId,
			ReceiverName = receiver.DisplayName,
			Text = message.Text,
			IsMine = true,
			IsRead = message.IsRead,
			SentAtUTC = message.SentAtUTC
		};

		var receiverDto = new MessageDto
		{
			MessageId = message.MessageId,
			SenderUserId = sender.UserId,
			SenderName = sender.DisplayName,
			ReceiverUserId = receiver.UserId,
			ReceiverName = receiver.DisplayName,
			Text = message.Text,
			IsMine = false,
			IsRead = message.IsRead,
			SentAtUTC = message.SentAtUTC
		};

		var notificationDto = new NotificationDto
		{
			NotificationId = notification.NotificationId,
			Title = notification.Title,
			Message = notification.Message,
			Type = notification.Type,
			Url = notification.Url,
			IsRead = notification.IsRead,
			CreatedAtUTC = notification.CreatedAtUTC
		};

		await _realtime.ReceiveMessageAsync(receiver.UserId, receiverDto);
		await _realtime.ReceiveMessageAsync(sender.UserId, senderDto);
		await _realtime.NotificationCreatedAsync(receiver.UserId, notificationDto);

		if (!_realtime.IsOnline(receiver.UserId))
		{
			await _notificationService.SendToUserAsync(
				receiver.UserId,
				"New message",
				$"{sender.DisplayName}: {preview}",
				new Dictionary<string, string>
				{
					{ "type", "message" },
					{ "senderUserId", sender.UserId.ToString() },
					{ "receiverUserId", receiver.UserId.ToString() },
					{ "messageId", message.MessageId.ToString() },
					{ "notificationId", notification.NotificationId.ToString() }
				});
		}

		return senderDto;
	}

	public async Task MarkAsReadAsync(string auth0Id, Guid messageId)
	{
		var me = await _context.Users
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted);

		if (me is null)
			throw new Exception("User not found.");

		var message = await _context.Messages
			.FirstOrDefaultAsync(x =>
				x.MessageId == messageId &&
				x.ReceiverUserId == me.UserId &&
				!x.IsDeleted);

		if (message is null)
			return;

		if (message.IsRead)
			return;

		message.IsRead = true;
		message.ReadAtUTC = DateTime.UtcNow;

		await _context.SaveChangesAsync();

		await _realtime.MessageReadAsync(
			message.SenderUserId,
			new MessageReadDto
			{
				MessageId = message.MessageId,
				ReaderUserId = me.UserId
			});
	}

	public async Task SendSystemMessageAsync(
	Guid receiverUserId,
	string title,
	string body)
	{
		var receiver = await _context.Users
			.FirstOrDefaultAsync(x =>
				x.UserId == receiverUserId &&
				!x.IsDeleted);

		if (receiver is null)
			return;

		var systemUser = await _context.Users
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == "system" &&
				!x.IsDeleted);

		if (systemUser is null)
		{
			systemUser = new backend.Entities.Identity.User
			{
				UserId = Guid.NewGuid(),
				Auth0Id = "system",
				Email = "system@eleaguefutbol.com",
				DisplayName = "eLeagueFutbol",
				AvatarUrl = "/images/logo-clear.png",
				TimeZone = "UTC",
				IsActive = true,
				Role = "System",
				CreatedAtUTC = DateTime.UtcNow
			};

			_context.Users.Add(systemUser);
			await _context.SaveChangesAsync();
		}

		var now = DateTime.UtcNow;

		var text = $"{title}\n\n{body}";

		var message = new Message
		{
			MessageId = Guid.NewGuid(),
			SenderUserId = systemUser.UserId,
			ReceiverUserId = receiver.UserId,
			Text = text,
			IsRead = false,
			SentAtUTC = now
		};

		var notification = new Notification
		{
			NotificationId = Guid.NewGuid(),
			UserId = receiver.UserId,
			Title = title,
			Message = body,
			Type = "draft_lottery",
			Url = "/draft",
			IsRead = false,
			CreatedAtUTC = now
		};

		_context.Messages.Add(message);
		_context.Notifications.Add(notification);

		await _context.SaveChangesAsync();

		var messageDto = new MessageDto
		{
			MessageId = message.MessageId,
			SenderUserId = systemUser.UserId,
			SenderName = systemUser.DisplayName,
			ReceiverUserId = receiver.UserId,
			ReceiverName = receiver.DisplayName,
			Text = message.Text,
			IsMine = false,
			IsRead = false,
			SentAtUTC = message.SentAtUTC
		};

		var notificationDto = new NotificationDto
		{
			NotificationId = notification.NotificationId,
			Title = notification.Title,
			Message = notification.Message,
			Type = notification.Type,
			Url = notification.Url,
			IsRead = notification.IsRead,
			CreatedAtUTC = notification.CreatedAtUTC
		};

		await _realtime.ReceiveMessageAsync(receiver.UserId, messageDto);
		await _realtime.NotificationCreatedAsync(receiver.UserId, notificationDto);

		if (!_realtime.IsOnline(receiver.UserId))
		{
			await _notificationService.SendToUserAsync(
				receiver.UserId,
				title,
				body,
				new Dictionary<string, string>
				{
					{ "type", "draft_lottery" },
					{ "receiverUserId", receiver.UserId.ToString() },
					{ "notificationId", notification.NotificationId.ToString() }
				});
		}
	}
}

namespace backend.Features.Messages.Services;

using backend.Data;
using backend.Entities.Messaging;
using backend.Features.Messages.Dtos;
using backend.Features.Notifications.Services;
using Microsoft.EntityFrameworkCore;

public class MessageService : IMessageService
{
	private readonly AppDbContext _context;
	private readonly INotificationService _notificationService;

	public MessageService(
		AppDbContext context,
		INotificationService notificationService)
	{
		_context = context;
		_notificationService = notificationService;
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

		var message = new Message
		{
			MessageId = Guid.NewGuid(),
			SenderUserId = sender.UserId,
			ReceiverUserId = receiver.UserId,
			Text = text,
			IsRead = false,
			SentAtUTC = DateTime.UtcNow
		};

		_context.Messages.Add(message);

		await _context.SaveChangesAsync();

		await _notificationService.SendToUserAsync(
			receiver.UserId,
			"New message",
			$"{sender.DisplayName}: {message.Text}",
			new Dictionary<string, string>
			{
				{ "type", "message" },
				{ "senderUserId", sender.UserId.ToString() },
				{ "messageId", message.MessageId.ToString() }
			});

		return new MessageDto
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
	}
}
namespace backend.Entities.Messaging;

using backend.Entities.Common;
using backend.Entities.Identity;

public class Message : BaseEntity
{
	public Guid MessageId { get; set; }

	public Guid SenderUserId { get; set; }

	public User SenderUser { get; set; } = null!;

	public Guid ReceiverUserId { get; set; }

	public User ReceiverUser { get; set; } = null!;

	public string Text { get; set; } = string.Empty;

	public bool IsRead { get; set; }

	public DateTime? ReadAtUTC { get; set; }

	public DateTime SentAtUTC { get; set; } = DateTime.UtcNow;
}
namespace backend.Features.Messages.Dtos;

public class MessageDto
{
	public Guid MessageId { get; set; }

	public Guid SenderUserId { get; set; }

	public string SenderName { get; set; } = string.Empty;

	public Guid ReceiverUserId { get; set; }

	public string ReceiverName { get; set; } = string.Empty;

	public string Text { get; set; } = string.Empty;

	public bool IsMine { get; set; }

	public bool IsRead { get; set; }

	public DateTime SentAtUTC { get; set; }
}
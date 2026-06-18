namespace backend.Features.Messages.Dtos;

public class SendMessageDto
{
	public Guid ReceiverUserId { get; set; }

	public string Text { get; set; } = string.Empty;
}
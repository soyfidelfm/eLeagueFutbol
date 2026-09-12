namespace backend.Features.Realtime;

public class MessageReadDto
{
	public Guid MessageId { get; set; }

	public Guid ReaderUserId { get; set; }
}

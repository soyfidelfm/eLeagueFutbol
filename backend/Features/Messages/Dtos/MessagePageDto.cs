namespace backend.Features.Messages.Dtos;

/// <summary>
/// One page of a conversation. Items are ordered oldest to newest.
/// HasMore is true when older messages exist before the first item.
/// </summary>
public class MessagePageDto
{
	public List<MessageDto> Items { get; set; } = [];

	public bool HasMore { get; set; }
}

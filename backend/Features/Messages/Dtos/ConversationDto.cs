namespace backend.Features.Messages.Dtos;

public class ConversationDto
{
	public Guid UserId { get; set; }

	public string DisplayName { get; set; } = string.Empty;

	public string? AvatarUrl { get; set; }

	public string LastMessage { get; set; } = string.Empty;

	public DateTime LastMessageAtUTC { get; set; }

	public int UnreadCount { get; set; }
}
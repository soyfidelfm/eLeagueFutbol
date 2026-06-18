namespace backend.Features.Notifications.Dtos;

public class NotificationDto
{
	public Guid NotificationId { get; set; }

	public string Title { get; set; } = string.Empty;

	public string Message { get; set; } = string.Empty;

	public string Type { get; set; } = string.Empty;

	public string? Url { get; set; }

	public bool IsRead { get; set; }

	public DateTime CreatedAtUTC { get; set; }
}
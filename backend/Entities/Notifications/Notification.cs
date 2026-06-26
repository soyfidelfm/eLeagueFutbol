namespace backend.Entities.Notifications;

using backend.Entities.Common;
using backend.Entities.Identity;

public class Notification : BaseEntity
{
	public Guid NotificationId { get; set; }

	public Guid UserId { get; set; }

	public User User { get; set; } = null!;

	public string Title { get; set; } = string.Empty;

	public string Message { get; set; } = string.Empty;

	public string Type { get; set; } = string.Empty;

	public string? Url { get; set; }

	public bool IsRead { get; set; }

	public DateTime? ReadAtUTC { get; set; }
}
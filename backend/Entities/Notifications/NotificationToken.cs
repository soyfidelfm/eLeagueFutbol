namespace backend.Entities.Notifications;

using backend.Entities.Common;
using backend.Entities.Identity;

public class NotificationToken : BaseEntity
{
	public Guid NotificationTokenId { get; set; }

	public Guid UserId { get; set; }

	public User User { get; set; } = null!;

	public string FcmToken { get; set; } = string.Empty;

	public string DeviceType { get; set; } = string.Empty;

	public DateTime LastUsedAtUTC { get; set; } = DateTime.UtcNow;
}
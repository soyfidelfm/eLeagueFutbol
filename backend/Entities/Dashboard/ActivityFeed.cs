using backend.Entities.Identity;

namespace backend.Entities.Dashboard;

public class ActivityFeed
{
	public Guid Id { get; set; }

	public Guid UserId { get; set; }

	public string Text { get; set; } = string.Empty;

	public DateTime CreatedAtUTC { get; set; }

	public User User { get; set; } = null!;
}
namespace backend.Features.Realtime;

public class PresenceUserDto
{
	public Guid UserId { get; set; }

	public string DisplayName { get; set; } = string.Empty;

	public string? AvatarUrl { get; set; }

	public DateTime? LastSeenAtUTC { get; set; }
}

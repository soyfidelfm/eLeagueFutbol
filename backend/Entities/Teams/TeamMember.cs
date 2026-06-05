namespace backend.Entities.Teams;

using backend.Entities.Common;
using backend.Entities.Identity;

public class TeamMember : BaseEntity

{
	public Guid TeamMemberId { get; set; }

	public Guid TeamId { get; set; }

	public Guid UserId { get; set; }

	public bool IsCaptain { get; set; }

	public DateTime JoinedAt { get; set; }

	// Navigation

	public Team Team { get; set; } = null!;

	public User User { get; set; } = null!;
}
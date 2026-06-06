namespace backend.Entities.Identity;

using backend.Entities.Common;
using backend.Entities.Stats;
using backend.Entities.Teams;

public class User : BaseEntity
{
	public Guid UserId { get; set; }

	public string Auth0Id { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public string DisplayName { get; set; } = string.Empty;

	public string AvatarUrl { get; set; } = string.Empty;

	public string TimeZone { get; set; } = "UTC";

	public DateTime? LastLoginAtUTC { get; set; }

	public bool IsActive { get; set; } = true;

	// Navigation

	public ICollection<TeamMember> TeamMemberships { get; set; }
		= new List<TeamMember>();

	public ICollection<MatchPlayerStats> MatchStats { get; set; }
		= new List<MatchPlayerStats>();
}
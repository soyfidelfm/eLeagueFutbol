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


	public bool IsActive { get; set; }

	// Navigation

	public ICollection<TeamMember> TeamMemberships { get; set; }
		= new List<TeamMember>();

	public ICollection<MatchPlayerStats> MatchStats { get; set; }
		= new List<MatchPlayerStats>();
}
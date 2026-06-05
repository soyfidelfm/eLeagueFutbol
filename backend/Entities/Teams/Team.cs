namespace backend.Entities.Teams;

using backend.Entities.Common;
using backend.Entities.Identity;

public class Team : BaseEntity

{
	public Guid TeamId { get; set; }

	public string Name { get; set; } = string.Empty;

	public string LogoUrl { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public Guid OwnerUserId { get; set; }

	// Navigation

	public User OwnerUser { get; set; } = null!;

	public ICollection<TeamMember> Members { get; set; }
		= new List<TeamMember>();
}
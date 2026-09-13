namespace backend.Features.Teams.Dtos;

public class TeamDto
{
	public Guid TeamId { get; set; }
	public string Name { get; set; } = string.Empty;
	public string LogoUrl { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public Guid OwnerUserId { get; set; }
	public string OwnerDisplayName { get; set; } = string.Empty;
	public Guid GameClubId { get; set; }
	public string GameClubName { get; set; } = string.Empty;
	public bool IsMine { get; set; }
	public bool IsOwner { get; set; }
	public List<TeamMemberDto> Members { get; set; } = [];
}

public class TeamMemberDto
{
	public Guid TeamMemberId { get; set; }
	public Guid UserId { get; set; }
	public string DisplayName { get; set; } = string.Empty;
	public string GamerTag { get; set; } = string.Empty;
	public string AvatarUrl { get; set; } = string.Empty;
	public bool IsCaptain { get; set; }
	public DateTime JoinedAt { get; set; }
}

public class CreateTeamDto
{
	public string Name { get; set; } = string.Empty;
	public string? LogoUrl { get; set; }
	public string? Description { get; set; }
}

public class AddTeamMemberDto
{
	public Guid UserId { get; set; }
}

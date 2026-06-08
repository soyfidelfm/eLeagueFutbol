namespace backend.Features.Users.Dtos;

public class MeUserDto
{
	public Guid UserId { get; set; }

	public string Auth0Id { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public string DisplayName { get; set; } = string.Empty;

	public string AvatarUrl { get; set; } = string.Empty;
	public string ClubName { get; set; } = string.Empty;
	public Guid? GameClubId { get; set; } = null;
	public string TimeZone { get; set; } = string.Empty;

	public string? GamerTag { get; set; }

	public bool IsActive { get; set; }

	public DateTime? LastLoginAtUTC { get; set; }

	public List<ClaimDto> Claims { get; set; } = new();
}

public class ClaimDto
{
	public string Type { get; set; } = string.Empty;
	public string Value { get; set; } = string.Empty;
}
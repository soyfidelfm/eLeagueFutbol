namespace backend.Features.Users.Dtos;

public class UserDto
{
	public Guid UserId { get; set; }

	public string Auth0Id { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public string DisplayName { get; set; } = string.Empty;

	public string AvatarUrl { get; set; } = string.Empty;

	public bool IsActive { get; set; }
	public string TimeZone { get; set; } = "UTC";

	public DateTime? LastLoginAtUTC { get; set; }
}

public class UpdateUserDto
{
	public string DisplayName { get; set; } = string.Empty;

	public string AvatarUrl { get; set; } = string.Empty;

	public string TimeZone { get; set; } = "UTC";
}
namespace backend.Features.Users.Dtos;

public class SyncUserDto
{
	public string Auth0Id { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public string DisplayName { get; set; } = string.Empty;

	public string AvatarUrl { get; set; } = string.Empty;

	public string TimeZone { get; set; } = "UTC";
}
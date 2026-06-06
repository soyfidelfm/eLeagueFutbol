namespace backend.Features.GameClubs.Dtos;

public class GameClubDto
{
	public Guid GameClubId { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Code { get; set; } = string.Empty;

	public string League { get; set; } = string.Empty;

	public string Country { get; set; } = string.Empty;

	public string LogoUrl { get; set; } = string.Empty;
}
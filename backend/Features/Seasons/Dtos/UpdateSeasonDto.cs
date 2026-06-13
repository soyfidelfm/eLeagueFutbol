namespace backend.Features.Seasons.Dtos;

public class UpdateSeasonDto
{
	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public DateTime StartDate { get; set; }

	public DateTime EndDate { get; set; }

	public string TimeZone { get; set; } = "UTC";
	public int TotalWeeks { get; set; }

	public bool IsCurrent { get; set; }

	public bool IsActive { get; set; }

	public bool RegistrationOpen { get; set; }

	public int MaxPlayers { get; set; }
}
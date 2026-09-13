namespace backend.Features.Seasons.Dtos;

public class SeasonDto
{
	public Guid SeasonId { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public DateTime StartDate { get; set; }

	public DateTime EndDate { get; set; }

	public bool IsCurrent { get; set; }

	public bool IsActive { get; set; }

	public bool RegistrationOpen { get; set; }

	public int MaxPlayers { get; set; }
	public int TotalWeeks { get; set; }

	public string Phase { get; set; } = "Registration";

	public int RegisteredCount { get; set; }

	public bool HasDraft { get; set; }

	public bool DraftActive { get; set; }

	public bool HasSchedule { get; set; }

	public DateTime CreatedAtUTC { get; set; }

	public DateTime? UpdatedAtUTC { get; set; }
}

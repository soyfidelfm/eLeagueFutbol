namespace backend.Entities.Tournaments;

using backend.Entities.Common;
public class Season : BaseEntity

{
	public Guid SeasonId { get; set; }

	public string Name { get; set; } = string.Empty;
	// Ejemplo:
	// Season 1
	// Summer 2026
	// FC26 Season 3

	public string Description { get; set; } = string.Empty;

	public DateTime StartDate { get; set; }

	public DateTime EndDate { get; set; }

	public bool IsCurrent { get; set; }

	public bool IsActive { get; set; }

	// Navigation

	public ICollection<Tournament> Tournaments { get; set; }
		= new List<Tournament>();
}
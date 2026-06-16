namespace backend.Entities.Tournaments;

using backend.Entities.Draft;
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
	public bool RegistrationOpen { get; set; } = false;
	public int MaxPlayers { get; set; } = 32;

	public int TotalWeeks { get; set; } = 8;

	public ICollection<SeasonRegistration> Registrations { get; set; }
		= new List<SeasonRegistration>();

	// Navigation

	public ICollection<Tournament> Tournaments { get; set; }
		= new List<Tournament>();

	public ICollection<DraftSession> DraftSessions { get; set; } = [];
	public ICollection<DraftOrder> DraftOrders { get; set; } = [];
	public ICollection<DraftPick> DraftPicks { get; set; } = [];
}
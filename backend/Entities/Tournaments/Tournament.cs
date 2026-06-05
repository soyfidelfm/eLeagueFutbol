using backend.Entities.Common;
using backend.Entities.Tournaments;

public class Tournament : BaseEntity

{
	public Guid TournamentId { get; set; }

	public Guid SeasonId { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public DateTime StartDate { get; set; }

	public DateTime EndDate { get; set; }

	public bool IsActive { get; set; }

	// Navigation

	public Season Season { get; set; } = null!;

	public ICollection<Match> Matches { get; set; }
		= new List<Match>();
}
namespace backend.Entities.Stats;

using backend.Entities.Common;
using backend.Entities.Identity;
using backend.Entities.Tournaments;

public class MatchPlayerStats : BaseEntity

{
	public Guid MatchPlayerStatsId { get; set; }

	public Guid MatchId { get; set; }

	public Guid UserId { get; set; }

	public int Goals { get; set; }

	public int Assists { get; set; }

	public decimal Rating { get; set; }

	// Navigation

	public Match Match { get; set; } = null!;

	public User User { get; set; } = null!;
}
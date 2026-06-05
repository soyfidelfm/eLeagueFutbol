using backend.Entities.Common;
using backend.Entities.Enums;
using backend.Entities.Stats;
using backend.Entities.Teams;

public class Match : BaseEntity
{
	public Guid MatchId { get; set; }

	public Guid TournamentId { get; set; }

	public Guid HomeTeamId { get; set; }

	public Guid AwayTeamId { get; set; }

	public int HomeScore { get; set; }

	public int AwayScore { get; set; }

	public DateTime MatchDate { get; set; }

	public MatchStatus Status { get; set; }

	public bool IsFeatured { get; set; }

	public Tournament Tournament { get; set; } = null!;

	public Team HomeTeam { get; set; } = null!;

	public Team AwayTeam { get; set; } = null!;

	public ICollection<MatchPlayerStats> PlayerStats { get; set; }
		= new List<MatchPlayerStats>();
}
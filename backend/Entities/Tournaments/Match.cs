using backend.Entities.Common;
using backend.Entities.Enums;
using backend.Entities.GameData;
using backend.Entities.Identity;
using backend.Entities.Stats;

namespace backend.Entities.Tournaments;

public class Match : BaseEntity
{
	public Guid MatchId { get; set; }

	public Guid TournamentId { get; set; }

	public Guid HomeUserId { get; set; }

	public Guid AwayUserId { get; set; }

	public Guid HomeGameClubId { get; set; }

	public Guid AwayGameClubId { get; set; }

	public int HomeScore { get; set; }

	public int AwayScore { get; set; }

	public DateTime MatchDate { get; set; }

	public MatchStatus Status { get; set; }

	public int WeekNumber { get; set; }

	public bool IsMatchOfTheWeek { get; set; }

	public string Arena { get; set; } = "Champions Arena";

	public string Mode { get; set; } = "Best of 1";

	public Tournament Tournament { get; set; } = null!;

	public User HomeUser { get; set; } = null!;

	public User AwayUser { get; set; } = null!;

	public GameClub HomeGameClub { get; set; } = null!;

	public GameClub AwayGameClub { get; set; } = null!;

	public ICollection<MatchPlayerStats> PlayerStats { get; set; }
		= new List<MatchPlayerStats>();
}
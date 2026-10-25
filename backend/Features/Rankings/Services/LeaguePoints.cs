using backend.Entities.Tournaments;

namespace backend.Features.Rankings.Services;

/// <summary>
/// Single source of truth for the league points system:
/// 3 points per win, 1 per draw, 0 per loss. Win rate = wins / played (draws count as played).
/// Used by the club table, the player table, the dashboard and the sidebar.
/// </summary>
public static class LeaguePoints
{
	public const int Win = 3;
	public const int Draw = 1;
	public const int Loss = 0;

	public sealed class Line
	{
		public int Wins { get; set; }
		public int Draws { get; set; }
		public int Losses { get; set; }
		public int GoalsFor { get; set; }
		public int GoalsAgainst { get; set; }
		public List<string> Form { get; } = [];

		public int Played => Wins + Draws + Losses;
		public int Points => Wins * Win + Draws * Draw + Losses * Loss;
		public int GoalDifference => GoalsFor - GoalsAgainst;
		public decimal WinRate => Played == 0 ? 0 : Math.Round((decimal)Wins / Played * 100, 2);
		public List<string> LastFive => Form.TakeLast(5).ToList();

		public void Add(int scored, int conceded)
		{
			GoalsFor += scored;
			GoalsAgainst += conceded;

			if (scored > conceded) { Wins++; Form.Add("W"); }
			else if (scored < conceded) { Losses++; Form.Add("L"); }
			else { Draws++; Form.Add("D"); }
		}
	}

	/// <summary>
	/// Builds a line per participant id from finished matches (caller filters status/season).
	/// keySelector maps a user id to the key of the table (user id or user club id); null skips.
	/// </summary>
	public static Dictionary<Guid, Line> Build(
		IEnumerable<Match> finishedMatches,
		Func<Guid, Guid?> keySelector)
	{
		var table = new Dictionary<Guid, Line>();

		foreach (var m in finishedMatches.OrderBy(x => x.MatchDate))
		{
			var home = keySelector(m.HomeUserId);
			var away = keySelector(m.AwayUserId);

			if (home is null || away is null)
				continue;

			Get(table, home.Value).Add(m.HomeScore, m.AwayScore);
			Get(table, away.Value).Add(m.AwayScore, m.HomeScore);
		}

		return table;
	}

	private static Line Get(Dictionary<Guid, Line> table, Guid key)
	{
		if (!table.TryGetValue(key, out var line))
		{
			line = new Line();
			table[key] = line;
		}

		return line;
	}
}

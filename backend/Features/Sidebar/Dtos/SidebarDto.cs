namespace backend.Features.Sidebar.Dtos;

public class SidebarDto
{
	public SidebarSeasonDto? Season { get; set; }
	public SidebarCupDto? Cup { get; set; }

	// Per-user data (null when anonymous or not registered in the season).
	public SidebarNextMatchDto? NextMatch { get; set; }
	public SidebarStandingDto? Standing { get; set; }
	public SidebarScorerDto? LeagueTopScorer { get; set; }
	public SidebarScorerDto? TeamTopScorer { get; set; }
	public SidebarAssistDto? LeagueTopAssist { get; set; }
	public SidebarAssistDto? TeamTopAssist { get; set; }

	/// <summary>Top 5 of the league table (3/1/0 points).</summary>
	public List<SidebarTableRowDto> TopTable { get; set; } = [];

	/// <summary>The user's row when they are outside the top 5.</summary>
	public SidebarTableRowDto? MyTableRow { get; set; }
}

public class SidebarSeasonDto
{
	public string Label { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public int TotalClubs { get; set; }
	public int TotalMatches { get; set; }

	public DateTime? StartsAtUtc { get; set; }
}

public class SidebarCupDto
{
	public string Name { get; set; } = string.Empty;
	public DateTime? StartsAtUtc { get; set; }
}

public class SidebarNextMatchDto
{
	public Guid MatchId { get; set; }
	public Guid SeasonId { get; set; }
	public int WeekNumber { get; set; }
	public string Opponent { get; set; } = string.Empty;
	public string OpponentLogoUrl { get; set; } = string.Empty;
	public bool IsHome { get; set; }
	public DateTime MatchDateUtc { get; set; }
	public string Stage { get; set; } = string.Empty;
}

public class SidebarStandingDto
{
	public int Position { get; set; }
	public int TotalClubs { get; set; }
	public int Points { get; set; }
	public string ClubName { get; set; } = string.Empty;
	public bool IsTopFive { get; set; }
}

public class SidebarScorerDto
{
	public string PlayerName { get; set; } = string.Empty;
	public string ClubName { get; set; } = string.Empty;
	public int Goals { get; set; }
}

public class SidebarAssistDto
{
	public string PlayerName { get; set; } = string.Empty;
	public string ClubName { get; set; } = string.Empty;
	public int Assists { get; set; }
}

public class SidebarTableRowDto
{
	public int Rank { get; set; }
	public Guid ClubId { get; set; }
	public string ClubName { get; set; } = string.Empty;
	public string OwnerName { get; set; } = string.Empty;
	public int Points { get; set; }
	public bool IsMe { get; set; }
}

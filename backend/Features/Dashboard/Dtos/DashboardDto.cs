namespace backend.Features.Dashboard.Dtos;

public class DashboardDto
{
	public List<KpiDto> Kpis { get; set; } = [];

	public List<PlayerLeaderboardDto> Leaderboard { get; set; } = [];

	public MatchOfWeekDto? MatchOfWeek { get; set; }

	public List<MatchSummaryDto> Matches { get; set; } = [];

	public List<UpcomingMatchDto> UpcomingMatches { get; set; } = [];

	public List<NewsItemDto> News { get; set; } = [];

	public FeaturedPlayerDto? PlayerOfWeek { get; set; }

	public MyStatsDto MyStats { get; set; } = new();

	public List<ActivityDto> Activities { get; set; } = [];
	public Guid? CurrentSeasonId { get; set; }

	public string? CurrentSeasonName { get; set; }
}

public class KpiDto
{
	public string Label { get; set; } = string.Empty;
	public string Value { get; set; } = string.Empty;
	public string Sub { get; set; } = string.Empty;
}

public class PlayerLeaderboardDto
{
	public int Rank { get; set; }
	public string Name { get; set; } = string.Empty;
	public string TeamName { get; set; } = string.Empty;
	public string TeamLogo { get; set; } = string.Empty;
	public bool IsMe { get; set; }
	public int Points { get; set; }
	public string Gd { get; set; } = string.Empty;
	public string Record { get; set; } = string.Empty;
	public string Avatar { get; set; } = string.Empty;
	public List<string> Form { get; set; } = [];
}

public class FeaturedPlayerDto
{
	public string Name { get; set; } = string.Empty;
	public int Rank { get; set; }
	public int Rating { get; set; }
	public string Image { get; set; } = string.Empty;
	public string Streak { get; set; } = string.Empty;
}

public class MatchOfWeekDto
{
	public string Arena { get; set; } = string.Empty;
	public string Stage { get; set; } = string.Empty;
	public string Date { get; set; } = string.Empty;
	public string Countdown { get; set; } = string.Empty;
	public FeaturedPlayerDto Home { get; set; } = new();
	public FeaturedPlayerDto Away { get; set; } = new();
}

public class MatchSummaryDto
{
	public string Stage { get; set; } = string.Empty;
	public string Score { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public string Home { get; set; } = string.Empty;
	public string Away { get; set; } = string.Empty;
	public string Date { get; set; } = string.Empty;
	/// <summary>W, D or L from the current user's point of view.</summary>
	public string Result { get; set; } = string.Empty;
}

public class UpcomingMatchDto
{
	public string Home { get; set; } = string.Empty;
	public string Away { get; set; } = string.Empty;
	public string Date { get; set; } = string.Empty;
	public string Stage { get; set; } = string.Empty;
}

public class NewsItemDto
{
	public string Title { get; set; } = string.Empty;
	public string Time { get; set; } = string.Empty;
}

public class MyStatsDto
{
	public string WinRate { get; set; } = "0%";
	public int Goals { get; set; }
	public int Assists { get; set; }
	public int CleanSheets { get; set; }
	public string AvgPossession { get; set; } = "0%";
	public int MatchesPlayed { get; set; }
	public int Wins { get; set; }
	public int Draws { get; set; }
	public int Losses { get; set; }
}

public class ActivityDto
{
	public string Name { get; set; } = string.Empty;
	public string Text { get; set; } = string.Empty;
	public string Time { get; set; } = string.Empty;
	public string Avatar { get; set; } = string.Empty;
}
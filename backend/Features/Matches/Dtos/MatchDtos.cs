using backend.Entities.Enums;

namespace backend.Features.Matches.Dtos;

public class MatchWeekDto
{
	public int Value { get; set; }

	public string Label { get; set; } = string.Empty;
}

public class UploadMatchStatsImageDto
{
	public MatchStatsUploadType Type { get; set; }

	public IFormFile Image { get; set; } = null!;
}

public class MatchStatsUploadResultDto
{
	public Guid MatchId { get; set; }

	public bool HasMatchSummary { get; set; }
	public bool HasHomeTeamPerformance { get; set; }
	public bool HasAwayTeamPerformance { get; set; }

	public bool IsReadyToProcess { get; set; }

	public string Status { get; set; } = string.Empty;
}

public class MatchTeamStatsDto
{
	public string TopScorer { get; set; } = "-";

	public int Goals { get; set; }

	public string TopAssist { get; set; } = "-";

	public int Assists { get; set; }

	public List<string> Streak { get; set; } = [];
}

public class MatchTeamDto
{
	public Guid UserId { get; set; }

	public Guid ClubId { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Club { get; set; } = string.Empty;

	public string LogoUrl { get; set; } = string.Empty;

	public MatchTeamStatsDto Stats { get; set; } = new();
}

public class MatchDto
{
	public Guid MatchId { get; set; }

	public int WeekNumber { get; set; }

	public bool IsMatchOfTheWeek { get; set; }

	public string Status { get; set; } = string.Empty;

	public string Arena { get; set; } = "Champions Arena";

	public string Mode { get; set; } = "Best of 1";

	public string MatchDate { get; set; } = string.Empty;

	public string MatchTime { get; set; } = string.Empty;

	public int? HomeScore { get; set; }

	public int? AwayScore { get; set; }

	public MatchTeamDto Home { get; set; } = new();

	public MatchTeamDto Away { get; set; } = new();
}

public class UploadMatchResultDto
{
	public int HomeScore { get; set; }

	public int AwayScore { get; set; }

	public string? Notes { get; set; }

	public string? ScreenshotUrl { get; set; }
}

public class MatchResultDto
{
	public Guid MatchId { get; set; }

	public int HomeScore { get; set; }

	public int AwayScore { get; set; }

	public string Status { get; set; } = string.Empty;
}
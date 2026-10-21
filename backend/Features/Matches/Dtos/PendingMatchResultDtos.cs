using backend.Features.Stats.Dtos;

namespace backend.Features.Matches.Dtos;

public class SubmitMatchResultProposalDto
{
	public IFormFile SummaryImage { get; set; } = null!;

	public IFormFile HomePerformanceImage { get; set; } = null!;

	public IFormFile AwayPerformanceImage { get; set; } = null!;
}

public class PendingMatchResultDto
{
	public Guid PendingMatchResultId { get; set; }

	public Guid MatchId { get; set; }

	public Guid SubmittedByUserId { get; set; }

	public string SubmittedByName { get; set; } = string.Empty;

	public string Status { get; set; } = string.Empty;

	public int HomeScore { get; set; }

	public int AwayScore { get; set; }

	public MatchSummaryStatsOcrDto? Summary { get; set; }

	public List<MatchPlayerStatsOcrPreviewDto> HomePlayers { get; set; } = [];

	public List<MatchPlayerStatsOcrPreviewDto> AwayPlayers { get; set; } = [];

	public DateTime CreatedAtUTC { get; set; }

	/// <summary>True when the current user submitted this proposal.</summary>
	public bool IsMine { get; set; }

	/// <summary>True when the current user may accept or reject it (opponent or admin, never the submitter).</summary>
	public bool CanReview { get; set; }
}

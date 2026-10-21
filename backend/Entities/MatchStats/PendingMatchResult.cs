using backend.Entities.Common;
using backend.Entities.Enums;
using backend.Entities.Tournaments;

namespace backend.Entities.MatchStats;

/// <summary>
/// A match result proposed from OCR screenshots. Nothing is written to the match
/// until the opponent (or an admin) accepts it.
/// </summary>
public class PendingMatchResult : BaseEntity
{
	public Guid PendingMatchResultId { get; set; }

	public Guid MatchId { get; set; }

	public Guid SubmittedByUserId { get; set; }

	public PendingMatchResultStatus Status { get; set; } = PendingMatchResultStatus.Pending;

	public int HomeScore { get; set; }

	public int AwayScore { get; set; }

	/// <summary>Serialized MatchSummaryStatsOcrDto.</summary>
	public string SummaryJson { get; set; } = string.Empty;

	/// <summary>Serialized TeamPerformanceOcrPreviewDto for the home team.</summary>
	public string HomePerformanceJson { get; set; } = string.Empty;

	/// <summary>Serialized TeamPerformanceOcrPreviewDto for the away team.</summary>
	public string AwayPerformanceJson { get; set; } = string.Empty;

	public Guid? ReviewedByUserId { get; set; }

	public DateTime? ReviewedAtUTC { get; set; }

	public Match Match { get; set; } = null!;
}

namespace backend.Application.Stats.DTOs;

public class ConfirmMatchPlayerStatsDto : MatchPlayerStatsOcrPreviewDto
{
	public Guid MatchId { get; set; }

	public Guid UserId { get; set; }

	public Guid TeamId { get; set; }
}
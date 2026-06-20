namespace backend.Features.Stats.Dtos;

public class ConfirmMatchPlayerStatsDto : MatchPlayerStatsOcrPreviewDto
{
	public Guid UserId { get; set; }

	public Guid TeamId { get; set; }
}
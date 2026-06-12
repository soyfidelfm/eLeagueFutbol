namespace backend.Application.MatchStats.DTOs;

public class ConfirmMatchPlayerStatsDto : MatchPlayerStatsOcrPreviewDto
{
	public Guid MatchId { get; set; }
	public Guid UserId { get; set; }
}
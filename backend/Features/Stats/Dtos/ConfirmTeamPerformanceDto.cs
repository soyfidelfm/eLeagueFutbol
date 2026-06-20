namespace backend.Features.Stats.Dtos;

public class ConfirmTeamPerformanceDto
{
	public Guid MatchId { get; set; }

	public List<ConfirmMatchPlayerStatsDto> Players { get; set; } = [];
}
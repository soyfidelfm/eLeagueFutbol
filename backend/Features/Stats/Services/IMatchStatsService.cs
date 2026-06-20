using backend.Features.Stats.Dtos;

namespace backend.Features.Stats.Services;

public interface IMatchStatsService
{
	Task SaveMatchSummaryAsync(
		ConfirmMatchSummaryDto dto);

	Task SaveTeamPerformanceAsync(
		ConfirmTeamPerformanceDto dto);
}
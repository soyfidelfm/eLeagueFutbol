using backend.Features.Stats.Dtos;

namespace backend.Features.Stats.Services;

public interface IMatchStatsOcrService
{
	Task<MatchSummaryStatsOcrDto> PreviewMatchSummaryAsync(
		IFormFile image);

	Task<TeamPerformanceOcrPreviewDto> PreviewTeamPerformanceAsync(
		IFormFile image);
}
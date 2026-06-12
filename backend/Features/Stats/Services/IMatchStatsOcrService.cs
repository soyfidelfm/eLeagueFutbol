using backend.Application.MatchStats.DTOs;

namespace backend.Features.Stats.Services;

public interface IMatchStatsOcrService
{
	Task<MatchPlayerStatsOcrPreviewDto> PreviewPlayerStatsAsync(IFormFile image);
}
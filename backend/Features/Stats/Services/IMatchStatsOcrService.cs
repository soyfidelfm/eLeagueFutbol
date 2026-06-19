using backend.Application.Stats.DTOs;
namespace backend.Features.Stats.Services;

public interface IMatchStatsOcrService
{
	Task<MatchPlayerStatsOcrPreviewDto> PreviewPlayerStatsAsync(IFormFile image);

	Task<MatchOcrPreviewDto> PreviewMatchScreenshotsAsync(List<IFormFile> images);
}
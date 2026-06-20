using backend.Application.Stats.DTOs;

namespace backend.Features.Stats.Dtos
{
	public class TeamPerformanceOcrPreviewDto
	{
		public List<MatchPlayerStatsOcrPreviewDto> Players { get; set; } = [];
		public List<string> RawLines { get; set; } = [];
		public List<string> Warnings { get; set; } = [];
	}
}

using backend.Features.Rankings.DTOs;

namespace backend.Features.Rankings.Services
{
	public interface IRankingService
	{
		Task<RankingsResponseDto> GetRankingsAsync(Guid? seasonId);
	}
}

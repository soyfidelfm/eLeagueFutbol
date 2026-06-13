using backend.Features.Matches.Dtos;

namespace backend.Features.Matches.Services;

public interface IMatchService
{
	Task<List<MatchWeekDto>> GetWeeksAsync(Guid seasonId);

	Task<List<MatchDto>> GetByWeekAsync(
		Guid seasonId,
		int weekNumber,
		string? timeZone
	);

	Task<MatchDto?> GetByIdAsync(
		Guid matchId,
		string? timeZone
	);

	Task<MatchResultDto?> UploadResultAsync(
		Guid matchId,
		UploadMatchResultDto dto
	);
	Task<bool> GenerateScheduleAsync(Guid seasonId);
}
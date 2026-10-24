using backend.Features.Matches.Dtos;

namespace backend.Features.Matches.Services;

public interface IMatchService
{
	Task<List<MatchWeekDto>> GetWeeksAsync(Guid seasonId);

	Task<List<MatchDto>> GetByWeekAsync(
		Guid seasonId,
		int weekNumber,
		string? timeZone);

	Task<MatchDto?> GetByIdAsync(
		Guid matchId,
		string? timeZone);

	Task<MatchResultDto?> UploadResultAsync(
		Guid matchId,
		UploadMatchResultDto dto);

	Task<MatchStatsUploadResultDto?> UploadStatsImageAsync(
		Guid matchId,
		UploadMatchStatsImageDto dto);

	Task<bool> GenerateScheduleAsync(Guid seasonId);
	Task<bool> SaveManualStatsAsync(
	Guid matchId,
	SaveManualMatchStatsDto dto);
	Task<SaveManualMatchStatsDto?> GetManualStatsAsync(Guid matchId);
	/// <summary>
	/// Stores the three OCR payloads as MatchStatsUploads and persists the stats
	/// (summary, player stats, score, Finished). Does not publish realtime events.
	/// </summary>
	Task ApplyOcrStatsAsync(
		Guid matchId,
		string summaryJson,
		string homePerformanceJson,
		string awayPerformanceJson);

	Task<MatchPlayerOptionsDto?> GetMatchPlayerOptionsAsync(
		Guid matchId);

}
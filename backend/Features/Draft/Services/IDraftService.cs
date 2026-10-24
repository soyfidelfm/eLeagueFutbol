namespace backend.Features.Draft.Services;

using backend.Entities.Enums;
using backend.Features.Draft.Dtos;

public interface IDraftService
{
	Task<DraftCenterDto?> GetCenterAsync(
		Guid seasonId,
		string auth0Id);

	Task<DraftCenterDto> GenerateDraftOrderAsync(
		Guid seasonId,
		string auth0Id,
		StartDraftDto dto);

	Task<DraftCenterDto> StartDraftAsync(
		Guid seasonId,
		string auth0Id);

	Task<List<DraftPlayerDto>> GetAvailablePlayersAsync(
	Guid seasonId,
	string? search,
	string? position,
	int page,
	int pageSize,
	DraftPlayerPoolFilter poolFilter = DraftPlayerPoolFilter.ClubOnly);

	Task<List<DraftPickDto>> GetPicksAsync(
		Guid seasonId);

	Task<DraftPickDto> MakePickAsync(
		Guid seasonId,
		string auth0Id,
		MakeDraftPickDto dto);

	Task<DraftLotteryPreviewDto> PreviewDraftOrderAsync(
		Guid seasonId,
		int drawNumber,
		string auth0Id);

	Task<DraftCenterDto> PauseDraftAsync(Guid seasonId, string auth0Id);

	Task<DraftCenterDto> ResumeDraftAsync(Guid seasonId, string auth0Id);

	Task<DraftCenterDto> StopDraftAsync(Guid seasonId, string auth0Id);

	Task<DraftCenterDto> UpdateTimerAsync(Guid seasonId, string auth0Id, UpdateDraftTimerDto dto);

	Task<DraftSettingsDto> GetDraftSettingsAsync();

	Task<DraftSettingsDto> UpdateDraftSettingsAsync(string auth0Id, DraftSettingsDto dto);
}

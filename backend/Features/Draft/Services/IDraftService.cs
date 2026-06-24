namespace backend.Features.Draft.Services;

using backend.Features.Draft.Dtos;

public interface IDraftService
{
	Task<DraftCenterDto?> GetCenterAsync(
		Guid seasonId,
		string auth0Id);

	Task<DraftCenterDto> StartDraftAsync(
	Guid seasonId,
	string auth0Id,
	StartDraftDto dto);

	Task<List<DraftPlayerDto>> GetAvailablePlayersAsync(
		Guid seasonId,
		string? search,
		string? position,
		int page,
		int pageSize);

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
}
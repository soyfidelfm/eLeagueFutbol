using backend.Features.Draft.Dtos;

namespace backend.Features.Draft.Services;

public interface IDraftService
{
	Task<DraftCenterDto?> GetCenterAsync(Guid seasonId, string auth0Id);

	Task<List<DraftPlayerDto>> GetAvailablePlayersAsync(
		Guid seasonId,
		string? search,
		string? position,
		int page,
		int pageSize);

	Task<List<DraftPickDto>> GetPicksAsync(Guid seasonId);

	Task<DraftPickDto> MakePickAsync(
		Guid seasonId,
		string auth0Id,
		MakeDraftPickDto dto);
}
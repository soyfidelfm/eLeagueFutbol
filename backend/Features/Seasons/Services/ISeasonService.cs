using backend.Features.Seasons.Dtos;

namespace backend.Features.Seasons.Services;

public interface ISeasonService
{
	Task<IReadOnlyList<SeasonDto>> GetAllAsync(CancellationToken ct);

	Task<SeasonDto?> GetByIdAsync(Guid id, CancellationToken ct);

	Task<SeasonDto?> GetCurrentAsync(CancellationToken ct);

	Task<SeasonDto> CreateAsync(
		CreateSeasonDto dto,
		CancellationToken ct);

	Task<bool> UpdateAsync(
		Guid id,
		UpdateSeasonDto dto,
		CancellationToken ct);

	Task<bool> DeleteAsync(
		Guid id,
		CancellationToken ct);

	Task<SeasonRegistrationDto> RegisterAsync(
		Guid seasonId,
		RegisterSeasonDto dto,
		string auth0Id,
		CancellationToken ct);

	Task<bool> IsUserRegisteredAsync(
		Guid seasonId,
		string auth0Id,
		CancellationToken ct);

	Task<SeasonDto> CloseRegistrationAsync(Guid id, CancellationToken ct);

	Task<SeasonDto> OpenRegistrationAsync(Guid id, CancellationToken ct);

	Task<SeasonDto> SetCurrentAsync(Guid id, CancellationToken ct);

	Task<SeasonDto> CloseSeasonAsync(Guid id, CancellationToken ct);

	Task<SeasonDto> ActivateSeasonAsync(Guid id, CancellationToken ct);
}

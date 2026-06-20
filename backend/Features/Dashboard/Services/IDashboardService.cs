using backend.Features.Dashboard.Dtos;

namespace backend.Features.Dashboard.Services;

public interface IDashboardService
{
	Task<DashboardDto> GetAsync(
		string auth0Id,
		string timeZone,
		CancellationToken ct);
}
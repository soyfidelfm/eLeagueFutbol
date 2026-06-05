using backend.Features.Dashboard.Dtos;

namespace backend.Features.Dashboard.Services;

public interface IDashboardService
{
	Task<DashboardDto> GetAsync(string timeZone, CancellationToken ct);
}
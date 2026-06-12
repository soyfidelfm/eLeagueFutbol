using eLeagueFutbol.Application.Stats.DTOs;

namespace eLeagueFutbol.Application.Stats;

public interface IStatsService
{
	Task<StatsOverviewDto> GetOverviewAsync();
}
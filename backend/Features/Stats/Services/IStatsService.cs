using backend.Application.Stats.DTOs;

namespace backend.Application.Stats;

public interface IStatsService
{
	Task<StatsOverviewDto> GetOverviewAsync();
}
namespace eLeagueFutbol.Application.Stats.DTOs;

public class StatsOverviewDto
{
	public List<StatsKpiDto> Kpis { get; set; } = [];
	public List<TopPlayerDto> TopPlayers { get; set; } = [];
	public List<PerformancePointDto> Performance { get; set; } = [];
	public GoalsBreakdownDto GoalsBreakdown { get; set; } = new();
}
using backend.Data;
using eLeagueFutbol.Application.Stats.DTOs;
using Microsoft.EntityFrameworkCore;

namespace eLeagueFutbol.Application.Stats;

public class StatsService : IStatsService
{
	private readonly AppDbContext _context;

	public StatsService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<StatsOverviewDto> GetOverviewAsync()
	{
		var totalMatches = await _context.Matches.CountAsync();

		var totalGoals = await _context.MatchPlayerStats.SumAsync(x => x.Goals);
		var totalAssists = await _context.MatchPlayerStats.SumAsync(x => x.Assists);
		var yellowCards = await _context.MatchPlayerStats.SumAsync(x => x.YellowCards);
		var redCards = await _context.MatchPlayerStats.SumAsync(x => x.RedCards);

		var topPlayersRaw = await _context.MatchPlayerStats
			.GroupBy(x => x.UserId)
			.Select(g => new
			{
				UserId = g.Key,
				Matches = g.Select(x => x.MatchId).Distinct().Count(),
				Goals = g.Sum(x => x.Goals),
				Assists = g.Sum(x => x.Assists),
				YellowCards = g.Sum(x => x.YellowCards),
				RedCards = g.Sum(x => x.RedCards),
				Rating = Math.Round(g.Average(x => x.Rating), 2)
			})
			.OrderByDescending(x => x.Rating)
			.ThenByDescending(x => x.Goals)
			.ThenByDescending(x => x.Assists)
			.Take(10)
			.ToListAsync();

		var userIds = topPlayersRaw.Select(x => x.UserId).ToList();

		var users = await _context.Users
			.Where(x => userIds.Contains(x.UserId))
			.ToListAsync();

		var memberships = await _context.TeamMembers
			.Include(x => x.Team)
			.Where(x => userIds.Contains(x.UserId))
			.ToListAsync();

		var topPlayers = topPlayersRaw
			.Select((x, index) =>
			{
				var user = users.FirstOrDefault(u => u.UserId == x.UserId);
				var membership = memberships.FirstOrDefault(m => m.UserId == x.UserId);

				return new TopPlayerDto
				{
					Rank = index + 1,
					UserId = x.UserId,
					GamerTag = user?.DisplayName ?? user?.Email ?? "Unknown Player",
					AvatarUrl = user?.AvatarUrl ?? "/images/avatar-default.png",
					ClubName = membership?.Team?.Name ?? "Free Agent",
					Matches = x.Matches,
					Goals = x.Goals,
					Assists = x.Assists,
					Rating = x.Rating
				};
			})
			.ToList();

		return new StatsOverviewDto
		{
			Kpis =
			[
				new StatsKpiDto
			{
				Label = "Matches Played",
				Value = totalMatches.ToString(),
				Icon = "fa-solid fa-gamepad"
			},
			new StatsKpiDto
			{
				Label = "Goals",
				Value = totalGoals.ToString(),
				Icon = "fa-solid fa-futbol"
			},
			new StatsKpiDto
			{
				Label = "Assists",
				Value = totalAssists.ToString(),
				Icon = "fa-solid fa-shoe-prints"
			},
			new StatsKpiDto
			{
				Label = "Yellow Cards",
				Value = yellowCards.ToString(),
				Icon = "fa-solid fa-square"
			},
			new StatsKpiDto
			{
				Label = "Red Cards",
				Value = redCards.ToString(),
				Icon = "fa-solid fa-square"
			}
			],
			TopPlayers = topPlayers,
			GoalsBreakdown = new GoalsBreakdownDto
			{
				TotalGoals = totalGoals,
				Assists = totalAssists,
				YellowCards = yellowCards,
				RedCards = redCards
			}
		};
	}

	private static decimal CalculateRating(int goals, int assists, int matches)
	{
		if (matches == 0)
			return 0;

		var rating = 6m + ((goals * 0.35m) + (assists * 0.25m)) / matches;

		return Math.Min(10m, Math.Round(rating, 2));
	}
}
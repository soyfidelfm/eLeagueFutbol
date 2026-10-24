using backend.Application.Stats.DTOs;
using backend.Data;
using backend.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace backend.Application.Stats;

public class StatsService : IStatsService
{
	private readonly AppDbContext _context;

	public StatsService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<StatsOverviewDto> GetOverviewAsync()
	{
		var totalMatches = await _context.Matches
			.CountAsync(x => !x.IsDeleted);

		var totalGoals = await _context.MatchPlayerStats
			.Where(x => !x.IsDeleted)
			.SumAsync(x => x.Goals);

		var totalAssists = await _context.MatchPlayerStats
			.Where(x => !x.IsDeleted)
			.SumAsync(x => x.Assists);

		var yellowCards = await _context.MatchPlayerStats
			.Where(x => !x.IsDeleted)
			.SumAsync(x => x.YellowCards);

		var redCards = await _context.MatchPlayerStats
			.Where(x => !x.IsDeleted)
			.SumAsync(x => x.RedCards);

		var topPlayersRaw = await _context.MatchPlayerStats
			.Where(x => !x.IsDeleted)
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

		var userIds = topPlayersRaw
			.Select(x => x.UserId)
			.ToList();

		var users = await _context.Users
			.AsNoTracking()
			.Where(x => userIds.Contains(x.UserId))
			.ToListAsync();

		var userClubs = await _context.UserClubs
			.AsNoTracking()
			.Include(x => x.BaseGameClub)
			.Where(x =>
				userIds.Contains(x.UserId) &&
				!x.IsDeleted)
			.ToListAsync();

		var topPlayers = topPlayersRaw
			.Select((x, index) =>
			{
				var user = users.FirstOrDefault(u => u.UserId == x.UserId);
				var userClub = userClubs.FirstOrDefault(c => c.UserId == x.UserId);

				return new TopPlayerDto
				{
					Rank = index + 1,
					UserId = x.UserId,
					GamerTag = user?.DisplayName ?? user?.Email ?? "Unknown Player",
					AvatarUrl = user?.AvatarUrl ?? "/images/avatar-default.png",
					ClubName = userClub?.Name
						?? userClub?.BaseGameClub?.Name
						?? user?.ClubName
						?? "Free Agent",
					Matches = x.Matches,
					Goals = x.Goals,
					Assists = x.Assists,
					Rating = x.Rating
				};
			})
			.ToList();

		// Real performance over last matches (aggregated)
		var recentMatches = await _context.Matches
			.AsNoTracking()
			.Where(m => !m.IsDeleted && m.Status == MatchStatus.Finished)
			.OrderByDescending(m => m.MatchDate)
			.Take(10)
			.Select(m => new { m.MatchId, m.MatchDate })
			.ToListAsync();

		var matchIds = recentMatches.Select(m => m.MatchId).ToList();

		var matchStats = await _context.MatchPlayerStats
			.AsNoTracking()
			.Where(s => matchIds.Contains(s.MatchId) && !s.IsDeleted)
			.GroupBy(s => s.MatchId)
			.Select(g => new
			{
				MatchId = g.Key,
				Goals = g.Sum(x => x.Goals),
				Assists = g.Sum(x => x.Assists),
				Rating = Math.Round(g.Average(x => x.Rating), 2)
			})
			.ToListAsync();

		var performance = recentMatches
			.OrderBy(m => m.MatchDate)
			.Select((m, index) =>
			{
				var stats = matchStats.FirstOrDefault(s => s.MatchId == m.MatchId);
				return new PerformancePointDto
				{
					Label = m.MatchDate.ToString("dd/MM"),
					Goals = stats?.Goals ?? 0,
					Assists = stats?.Assists ?? 0,
					Rating = stats?.Rating ?? 0
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
			Performance = performance,
			GoalsBreakdown = new GoalsBreakdownDto
			{
				TotalGoals = totalGoals,
				Assists = totalAssists,
				YellowCards = yellowCards,
				RedCards = redCards
			}
		};
	}
}

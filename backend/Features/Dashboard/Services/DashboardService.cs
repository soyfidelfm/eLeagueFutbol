using backend.Common.Helpers;
using backend.Data;
using backend.Entities.Dashboard;
using backend.Entities.Enums;
using backend.Entities.Tournaments;
using backend.Features.Dashboard.Dtos;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.Dashboard.Services;

public class DashboardService : IDashboardService
{
	private readonly AppDbContext _context;

	public DashboardService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<DashboardDto> GetAsync(string timeZone, CancellationToken ct)
	{
		var weekStartUtc = DateTimeHelper.GetStartOfWeekUtc(timeZone);
		var weekEndUtc = weekStartUtc.AddDays(7);

		var currentSeason = await _context.Seasons
			.Where(x => x.IsActive && x.IsCurrent && !x.IsDeleted)
			.OrderByDescending(x => x.StartDate)
			.FirstOrDefaultAsync(ct);

		currentSeason ??= await _context.Seasons
			.Where(x => x.IsActive && !x.IsDeleted)
			.OrderByDescending(x => x.StartDate)
			.FirstOrDefaultAsync(ct);

		var registered = currentSeason is null
			? 0
			: await _context.SeasonRegistrations
				.CountAsync(x => x.SeasonId == currentSeason.SeasonId, ct);

		var teamsCount = await _context.Teams
			.CountAsync(ct);

		var liveMatchesCount = await _context.Matches
			.CountAsync(x => x.Status == MatchStatus.Live, ct);

		var matchesThisWeekCount = await _context.Matches
			.CountAsync(x =>
				x.MatchDate >= weekStartUtc &&
				x.MatchDate < weekEndUtc,
				ct);

		var rankings = await _context.PlayerRankings
			.Include(x => x.User)
			.OrderByDescending(x => x.Points)
			.ThenByDescending(x => x.GoalDifference)
			.Take(10)
			.ToListAsync(ct);

		var featuredMatch = await _context.Matches
			.Include(x => x.Tournament)
			.Include(x => x.HomeTeam)
			.Include(x => x.AwayTeam)
			.Where(x => x.IsFeatured)
			.OrderBy(x => x.MatchDate)
			.FirstOrDefaultAsync(ct);

		var activeMatches = await _context.Matches
			.Include(x => x.Tournament)
			.Include(x => x.HomeTeam)
			.Include(x => x.AwayTeam)
			.Where(x =>
				x.Status == MatchStatus.Live ||
				x.Status == MatchStatus.WarmingUp ||
				x.Status == MatchStatus.Scheduled)
			.OrderBy(x => x.MatchDate)
			.Take(3)
			.ToListAsync(ct);

		var upcomingMatches = await _context.Matches
			.Include(x => x.Tournament)
			.Include(x => x.HomeTeam)
			.Include(x => x.AwayTeam)
			.Where(x => x.Status == MatchStatus.Scheduled)
			.OrderBy(x => x.MatchDate)
			.Take(4)
			.ToListAsync(ct);

		var news = await _context.NewsArticles
			.Where(x => x.IsPublished)
			.OrderByDescending(x => x.CreatedAtUTC)
			.Take(4)
			.Select(x => new NewsItemDto
			{
				Title = x.Title,
				Time = DateTimeHelper.GetRelativeTime(x.CreatedAtUTC)
			})
			.ToListAsync(ct);

		var activities = await _context.ActivityFeeds
			.Include(x => x.User)
			.OrderByDescending(x => x.CreatedAtUTC)
			.Take(5)
			.Select(x => new ActivityDto
			{
				Name = string.IsNullOrWhiteSpace(x.User.DisplayName)
					? x.User.Email
					: x.User.DisplayName,

				Text = x.Text,

				Time = DateTimeHelper.GetRelativeTime(x.CreatedAtUTC),

				Avatar = string.IsNullOrWhiteSpace(x.User.AvatarUrl)
					? "/images/avatar1.png"
					: x.User.AvatarUrl
			})
			.ToListAsync(ct);

		var topPlayer = rankings.FirstOrDefault();

		return new DashboardDto
		{
			CurrentSeasonId = currentSeason?.SeasonId,
			CurrentSeasonName = currentSeason?.Name,

			Kpis =
			[
				new()
			{
				Label = "Season",
				Value = currentSeason?.Name ?? "No Season",
				Sub = "Current Season"
			},
			new()
			{
				Label = "Players",
				Value = registered.ToString("N0"),
				Sub = "Registered"
			},
			//new()
			//{
			//	Label = "Teams",
			//	Value = teamsCount.ToString("N0"),
			//	Sub = "Active Clubs"
			//},
			new()
			{
				Label = "Live Now",
				Value = liveMatchesCount.ToString("N0"),
				Sub = "Matches"
			},
			new()
			{
				Label = "This Week",
				Value = matchesThisWeekCount.ToString("N0"),
				Sub = "Scheduled"
			}
			],

			Leaderboard = rankings
				.Select((x, index) => new PlayerLeaderboardDto
				{
					Rank = index + 1,

					Name = string.IsNullOrWhiteSpace(x.User.DisplayName)
						? x.User.Email
						: x.User.DisplayName,

					Points = x.Points,

					Gd = x.GoalDifference >= 0
						? $"+{x.GoalDifference}"
						: x.GoalDifference.ToString(),

					Record = $"{x.Wins}/{x.Losses}",

					Avatar = string.IsNullOrWhiteSpace(x.User.AvatarUrl)
						? "/images/avatar1.png"
						: x.User.AvatarUrl,

					Form = string.IsNullOrWhiteSpace(x.Form)
						? []
						: x.Form
							.Split(',', StringSplitOptions.RemoveEmptyEntries)
							.Select(f => f.Trim())
							.ToList()
				})
				.ToList(),

			MatchOfWeek = featuredMatch is null
				? null
				: BuildMatchOfWeek(featuredMatch, timeZone),

			Matches = activeMatches
				.Select(x => new MatchSummaryDto
				{
					Stage = x.Tournament?.Name ?? "Match",
					Score = $"{x.HomeScore} - {x.AwayScore}",
					Status = x.Status.ToString().ToUpper()
				})
				.ToList(),

			UpcomingMatches = upcomingMatches
				.Select(x => new UpcomingMatchDto
				{
					Home = x.HomeTeam.Name,
					Away = x.AwayTeam.Name,
					Date = DateTimeHelper.FormatLocal(x.MatchDate, timeZone),
					Stage = x.Tournament?.Name ?? "Scheduled"
				})
				.ToList(),

			News = news,

			PlayerOfWeek = topPlayer is null
				? null
				: new FeaturedPlayerDto
				{
					Name = string.IsNullOrWhiteSpace(topPlayer.User.DisplayName)
						? topPlayer.User.Email
						: topPlayer.User.DisplayName,

					Rank = 1,
					Rating = Math.Min(99, 80 + topPlayer.Wins),
					Image = string.IsNullOrWhiteSpace(topPlayer.User.AvatarUrl)
						? "/images/player1.png"
						: topPlayer.User.AvatarUrl,
					Streak = $"{topPlayer.Wins} Wins · {topPlayer.Points} PTS"
				},

			MyStats = new MyStatsDto
			{
				WinRate = "0%",
				Goals = 0,
				Assists = 0,
				CleanSheets = 0,
				AvgPossession = "0%"
			},

			Activities = activities
		};
	}

	private static MatchOfWeekDto BuildMatchOfWeek(
		Match match,
		string timeZone)
	{
		return new MatchOfWeekDto
		{
			Arena = match.Tournament?.Name ?? "Champions Arena",
			Stage = match.Status.ToString(),
			Date = DateTimeHelper.FormatLocal(
				match.MatchDate,
				timeZone,
				"dddd, h:mm tt"),
			Countdown = DateTimeHelper.GetCountdown(
				match.MatchDate,
				timeZone),
			Home = new FeaturedPlayerDto
			{
				Name = match.HomeTeam.Name,
				Rank = 0,
				Rating = 0,
				Image = "",
				Streak = $"{match.HomeScore} Goals"
			},
			Away = new FeaturedPlayerDto
			{
				Name = match.AwayTeam.Name,
				Rank = 0,
				Rating = 0,
				Image = "",
				Streak = $"{match.AwayScore} Goals"
			}
		};
	}
}
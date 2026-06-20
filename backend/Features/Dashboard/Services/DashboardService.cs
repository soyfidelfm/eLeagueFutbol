using backend.Common.Helpers;
using backend.Data;
using backend.Entities.Clubs;
using backend.Entities.Dashboard;
using backend.Entities.Enums;
using backend.Entities.GameData;
using backend.Entities.Identity;
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

	public async Task<DashboardDto> GetAsync(
		string auth0Id,
		string timeZone,
		CancellationToken ct)
	{
		var user = await _context.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				x.IsActive &&
				!x.IsDeleted,
				ct);

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

		var myUserClub = user is null || currentSeason is null
			? null
			: await _context.UserClubs
				.AsNoTracking()
				.Include(x => x.BaseGameClub)
				.FirstOrDefaultAsync(x =>
					x.UserId == user.UserId &&
					x.SeasonId == currentSeason.SeasonId &&
					!x.IsDeleted,
					ct);

		var userClubs = currentSeason is null
			? new Dictionary<Guid, UserClub>()
			: await _context.UserClubs
				.AsNoTracking()
				.Include(x => x.BaseGameClub)
				.Where(x =>
					x.SeasonId == currentSeason.SeasonId &&
					!x.IsDeleted)
				.ToDictionaryAsync(x => x.UserId, ct);

		var registered = currentSeason is null
			? 0
			: await _context.SeasonRegistrations
				.CountAsync(x => x.SeasonId == currentSeason.SeasonId, ct);

		var liveMatchesCount = await _context.Matches
			.CountAsync(x =>
				!x.IsDeleted &&
				x.Status == MatchStatus.Live,
				ct);

		var matchesThisWeekCount = await _context.Matches
			.CountAsync(x =>
				!x.IsDeleted &&
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
			.Include(x => x.HomeUser)
			.Include(x => x.AwayUser)
			.Include(x => x.HomeGameClub)
			.Include(x => x.AwayGameClub)
			.Where(x =>
				!x.IsDeleted &&
				x.IsMatchOfTheWeek)
			.OrderBy(x => x.MatchDate)
			.FirstOrDefaultAsync(ct);

		var activeMatches = await _context.Matches
			.Include(x => x.Tournament)
			.Include(x => x.HomeUser)
			.Include(x => x.AwayUser)
			.Include(x => x.HomeGameClub)
			.Include(x => x.AwayGameClub)
			.Where(x =>
				!x.IsDeleted &&
				(
					x.Status == MatchStatus.Live ||
					x.Status == MatchStatus.WarmingUp ||
					x.Status == MatchStatus.Scheduled
				))
			.OrderBy(x => x.MatchDate)
			.Take(3)
			.ToListAsync(ct);

		var upcomingMatches = await _context.Matches
			.Include(x => x.Tournament)
			.Include(x => x.HomeUser)
			.Include(x => x.AwayUser)
			.Include(x => x.HomeGameClub)
			.Include(x => x.AwayGameClub)
			.Where(x =>
				!x.IsDeleted &&
				x.Status == MatchStatus.Scheduled)
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

		var mySquadCount = myUserClub is null
			? 0
			: await _context.UserClubPlayers
				.CountAsync(x =>
					x.UserClubId == myUserClub.UserClubId &&
					!x.IsDeleted,
					ct);

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
				new()
				{
					Label = "Budget",
					Value = myUserClub is null
						? "$0"
						: $"${myUserClub.CurrentBudget / 1_000_000m:N1}M",
					Sub = "My Club"
				},
				new()
				{
					Label = "Squad",
					Value = $"{mySquadCount}/25",
					Sub = "Players"
				}
			],

			Leaderboard = rankings
				.Select((x, index) =>
				{
					userClubs.TryGetValue(x.UserId, out var club);

					return new PlayerLeaderboardDto
					{
						Rank = index + 1,

						Name = club?.Name ??
							(string.IsNullOrWhiteSpace(x.User.DisplayName)
								? x.User.Email
								: x.User.DisplayName),

						Points = x.Points,

						Gd = x.GoalDifference >= 0
							? $"+{x.GoalDifference}"
							: x.GoalDifference.ToString(),

						Record = $"{x.Wins}/{x.Losses}",

						Avatar = !string.IsNullOrWhiteSpace(club?.LogoUrl)
							? club.LogoUrl
							: !string.IsNullOrWhiteSpace(club?.BaseGameClub?.LogoUrl)
								? club.BaseGameClub.LogoUrl
								: string.IsNullOrWhiteSpace(x.User.AvatarUrl)
									? "/images/avatar1.png"
									: x.User.AvatarUrl,

						Form = string.IsNullOrWhiteSpace(x.Form)
							? []
							: x.Form
								.Split(',', StringSplitOptions.RemoveEmptyEntries)
								.Select(f => f.Trim())
								.ToList()
					};
				})
				.ToList(),

			MatchOfWeek = featuredMatch is null
				? null
				: BuildMatchOfWeek(featuredMatch, timeZone, userClubs),

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
					Home = GetClubName(
						x.HomeUserId,
						x.HomeUser,
						x.HomeGameClub,
						userClubs),

					Away = GetClubName(
						x.AwayUserId,
						x.AwayUser,
						x.AwayGameClub,
						userClubs),

					Date = DateTimeHelper.FormatLocal(x.MatchDate, timeZone),
					Stage = x.Tournament?.Name ?? "Scheduled"
				})
				.ToList(),

			News = news,

			PlayerOfWeek = topPlayer is null
				? null
				: new FeaturedPlayerDto
				{
					Name = userClubs.TryGetValue(topPlayer.UserId, out var topClub)
						? topClub.Name
						: string.IsNullOrWhiteSpace(topPlayer.User.DisplayName)
							? topPlayer.User.Email
							: topPlayer.User.DisplayName,

					Rank = 1,
					Rating = Math.Min(99, 80 + topPlayer.Wins),

					Image = userClubs.TryGetValue(topPlayer.UserId, out var playerClub)
						? !string.IsNullOrWhiteSpace(playerClub.LogoUrl)
							? playerClub.LogoUrl
							: playerClub.BaseGameClub?.LogoUrl ?? "/images/player1.png"
						: string.IsNullOrWhiteSpace(topPlayer.User.AvatarUrl)
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
		string timeZone,
		Dictionary<Guid, UserClub> userClubs)
	{
		var homeName = GetClubName(
			match.HomeUserId,
			match.HomeUser,
			match.HomeGameClub,
			userClubs);

		var awayName = GetClubName(
			match.AwayUserId,
			match.AwayUser,
			match.AwayGameClub,
			userClubs);

		var homeImage = GetClubImage(
			match.HomeUserId,
			match.HomeUser,
			match.HomeGameClub,
			userClubs);

		var awayImage = GetClubImage(
			match.AwayUserId,
			match.AwayUser,
			match.AwayGameClub,
			userClubs);

		return new MatchOfWeekDto
		{
			Arena = !string.IsNullOrWhiteSpace(match.HomeGameClub.StadiumName)
				? match.HomeGameClub.StadiumName
				: !string.IsNullOrWhiteSpace(match.AwayGameClub.StadiumName)
					? match.AwayGameClub.StadiumName
					: "Champions Arena",

			Stage = match.Tournament?.Name ?? match.Status.ToString(),

			Date = DateTimeHelper.FormatLocal(
				match.MatchDate,
				timeZone,
				"dddd, h:mm tt"),

			Countdown = DateTimeHelper.GetCountdown(
				match.MatchDate,
				timeZone),

			Home = new FeaturedPlayerDto
			{
				Name = homeName,
				Rank = 0,
				Rating = match.HomeGameClub.OverallRating,
				Image = homeImage,
				Streak = $"{match.HomeScore} Goals"
			},

			Away = new FeaturedPlayerDto
			{
				Name = awayName,
				Rank = 0,
				Rating = match.AwayGameClub.OverallRating,
				Image = awayImage,
				Streak = $"{match.AwayScore} Goals"
			}
		};
	}

	private static string GetClubName(
		Guid? userId,
		User? user,
		GameClub gameClub,
		Dictionary<Guid, UserClub> userClubs)
	{
		if (userId.HasValue && userClubs.TryGetValue(userId.Value, out var club))
			return club.Name;

		if (!string.IsNullOrWhiteSpace(user?.ClubName))
			return user.ClubName;

		return gameClub.Name;
	}

	private static string GetClubImage(
		Guid? userId,
		User? user,
		GameClub gameClub,
		Dictionary<Guid, UserClub> userClubs)
	{
		if (userId.HasValue && userClubs.TryGetValue(userId.Value, out var club))
		{
			if (!string.IsNullOrWhiteSpace(club.LogoUrl))
				return club.LogoUrl;

			if (!string.IsNullOrWhiteSpace(club.BaseGameClub?.LogoUrl))
				return club.BaseGameClub.LogoUrl;
		}

		if (!string.IsNullOrWhiteSpace(user?.AvatarUrl))
			return user.AvatarUrl;

		return gameClub.LogoUrl ?? string.Empty;
	}
}
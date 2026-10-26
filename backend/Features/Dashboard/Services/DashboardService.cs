using backend.Common.Helpers;
using backend.Data;
using backend.Entities.Clubs;
using backend.Entities.Dashboard;
using backend.Entities.Enums;
using backend.Entities.GameData;
using backend.Entities.Identity;
using backend.Entities.Tournaments;
using backend.Features.Dashboard.Dtos;
using backend.Features.Rankings.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.Dashboard.Services;

public class DashboardService : IDashboardService
{
	private readonly AppDbContext _context;
	private readonly IRankingService _rankingService;

	public DashboardService(AppDbContext context, IRankingService rankingService)
	{
		_context = context;
		_rankingService = rankingService;
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
				x.IsMatchOfTheWeek &&
				(currentSeason == null || x.Tournament.SeasonId == currentSeason.SeasonId))
			// Prefer this week's / the next featured match; otherwise the most recent one.
			.OrderBy(x => x.MatchDate >= weekStartUtc ? 0 : 1)
			.ThenBy(x => x.MatchDate >= weekStartUtc ? x.MatchDate : DateTime.MaxValue)
			.ThenByDescending(x => x.MatchDate)
			.FirstOrDefaultAsync(ct);

		// Matches of the current user in the current season (home or away).
		var myMatches = user is null || currentSeason is null
			? new List<Match>()
			: await _context.Matches
				.AsNoTracking()
				.Include(x => x.Tournament)
				.Include(x => x.HomeUser)
				.Include(x => x.AwayUser)
				.Include(x => x.HomeGameClub)
				.Include(x => x.AwayGameClub)
				.Include(x => x.MatchSummaryStats)
				.Where(x =>
					!x.IsDeleted &&
					x.Tournament.SeasonId == currentSeason.SeasonId &&
					(x.HomeUserId == user.UserId || x.AwayUserId == user.UserId))
				.ToListAsync(ct);

		var myFinished = myMatches
			.Where(x => x.Status == MatchStatus.Finished)
			.OrderByDescending(x => x.MatchDate)
			.ToList();

		// Last 3 results replace the old "active matches" (no live feed from the game).
		var lastResults = myFinished.Take(3).ToList();

		// Only the user's matches that have not been played yet.
		var upcomingMatches = myMatches
			.Where(x =>
				x.Status != MatchStatus.Finished &&
				x.Status != MatchStatus.Cancelled)
			.OrderBy(x => x.MatchDate)
			.Take(4)
			.ToList();

		// Standings (same logic as /api/rankings) for the Elite Leaderboard.
		var standings = currentSeason is null
			? null
			: await _rankingService.GetRankingsAsync(currentSeason.SeasonId);

		var clubsById = userClubs.Values
			.GroupBy(x => x.UserClubId)
			.ToDictionary(g => g.Key, g => g.First());

		var usersById = userClubs.Count == 0
			? new Dictionary<Guid, User>()
			: await _context.Users
				.AsNoTracking()
				.Where(x => userClubs.Keys.Contains(x.UserId))
				.ToDictionaryAsync(x => x.UserId, ct);

		var myStats = BuildMyStats(user?.UserId, myFinished);

		MatchOfWeekDto? matchOfWeek = null;

		if (featuredMatch is not null)
		{
			matchOfWeek = BuildMatchOfWeek(featuredMatch, timeZone, userClubs);

			var seasonId = featuredMatch.Tournament?.SeasonId ?? currentSeason?.SeasonId;

			matchOfWeek.Home.Rating = await GetSquadAverageAsync(
				featuredMatch.HomeUserId, seasonId, featuredMatch.HomeGameClub, ct);

			matchOfWeek.Away.Rating = await GetSquadAverageAsync(
				featuredMatch.AwayUserId, seasonId, featuredMatch.AwayGameClub, ct);
		}

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

			Leaderboard = (standings?.ClubRankings ?? [])
				.Take(10)
				.Select(x =>
				{
					clubsById.TryGetValue(x.ClubId, out var club);

					User? owner = null;
					if (club is not null)
						usersById.TryGetValue(club.UserId, out owner);

					var playerName = owner is null
						? x.ClubName
						: !string.IsNullOrWhiteSpace(owner.DisplayName)
							? owner.DisplayName
							: !string.IsNullOrWhiteSpace(owner.GamerTag)
								? owner.GamerTag
								: owner.Email;

					return new PlayerLeaderboardDto
					{
						Rank = x.Rank,
						Name = playerName,
						TeamName = x.ClubName,
						TeamLogo = x.ClubLogoUrl,
						IsMe = user is not null && club?.UserId == user.UserId,
						Points = x.Points,
						Gd = x.GoalDifference >= 0
							? $"+{x.GoalDifference}"
							: x.GoalDifference.ToString(),
						Record = $"{x.Wins}/{x.Draws}/{x.Losses}",
						Avatar = owner?.AvatarUrl ?? string.Empty,
						Form = x.Form
					};
				})
				.ToList(),

			MatchOfWeek = matchOfWeek,

			Matches = lastResults
				.Select(x =>
				{
					var isHome = x.HomeUserId == user!.UserId;
					var mine = isHome ? x.HomeScore : x.AwayScore;
					var theirs = isHome ? x.AwayScore : x.HomeScore;

					return new MatchSummaryDto
					{
						Stage = x.Tournament?.Name ?? "Match",
						Score = $"{x.HomeScore} - {x.AwayScore}",
						Status = x.Status.ToString().ToUpper(),
						Home = GetClubName(x.HomeUserId, x.HomeUser, x.HomeGameClub, userClubs),
						Away = GetClubName(x.AwayUserId, x.AwayUser, x.AwayGameClub, userClubs),
						Date = DateTimeHelper.FormatLocal(x.MatchDate, timeZone),
						Result = mine > theirs ? "W" : mine < theirs ? "L" : "D"
					};
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

					Image = userClubs.TryGetValue(topPlayer.UserId, out var playerClub) &&
		!string.IsNullOrWhiteSpace(playerClub.LogoUrl)
	? playerClub.LogoUrl
	: string.IsNullOrWhiteSpace(topPlayer.User.AvatarUrl)
		? "/images/player1.png"
		: topPlayer.User.AvatarUrl,

					Streak = $"{topPlayer.Wins} Wins · {topPlayer.Points} PTS"
				},

			MyStats = myStats,

			Activities = activities
		};
	}

	private MyStatsDto BuildMyStats(Guid? userId, List<Match> finished)
	{
		if (userId is null || finished.Count == 0)
			return new MyStatsDto { WinRate = "—", AvgPossession = "—" };

		int wins = 0, draws = 0, losses = 0, goals = 0, cleanSheets = 0;
		var possession = new List<int>();

		foreach (var m in finished)
		{
			var isHome = m.HomeUserId == userId;
			var mine = isHome ? m.HomeScore : m.AwayScore;
			var theirs = isHome ? m.AwayScore : m.HomeScore;

			goals += mine;
			if (theirs == 0) cleanSheets++;

			if (mine > theirs) wins++;
			else if (mine < theirs) losses++;
			else draws++;

			if (m.MatchSummaryStats is not null)
			{
				var p = isHome ? m.MatchSummaryStats.HomePossession : m.MatchSummaryStats.AwayPossession;
				if (p > 0) possession.Add(p);
			}
		}

		var matchIds = finished.Select(x => x.MatchId).ToList();

		var assists = _context.MatchPlayerStats
			.AsNoTracking()
			.Where(x =>
				!x.IsDeleted &&
				x.UserId == userId &&
				matchIds.Contains(x.MatchId))
			.Sum(x => (int?)x.Assists) ?? 0;

		return new MyStatsDto
		{
			MatchesPlayed = finished.Count,
			Wins = wins,
			Draws = draws,
			Losses = losses,
			WinRate = $"{Math.Round((decimal)wins / finished.Count * 100)}%",
			Goals = goals,
			Assists = assists,
			CleanSheets = cleanSheets,
			AvgPossession = possession.Count == 0
				? "—"
				: $"{Math.Round(possession.Average())}%"
		};
	}

	/// <summary>
	/// Average overall of the squad: the user's drafted/bought players for the season,
	/// falling back to the base game club's roster, then to the club's stored rating.
	/// </summary>
	private async Task<int> GetSquadAverageAsync(
		Guid userId,
		Guid? seasonId,
		GameClub gameClub,
		CancellationToken ct)
	{
		if (seasonId.HasValue)
		{
			var squad = await _context.UserClubPlayers
				.AsNoTracking()
				.Where(x =>
					!x.IsDeleted &&
					x.UserClub.UserId == userId &&
					x.UserClub.SeasonId == seasonId.Value &&
					!x.UserClub.IsDeleted)
				.Select(x => x.GamePlayer.Overall)
				.ToListAsync(ct);

			if (squad.Count > 0)
				return (int)Math.Round(squad.Average());
		}

		var roster = await _context.GamePlayers
			.AsNoTracking()
			.Where(x => !x.IsDeleted && x.GameClubId == gameClub.GameClubId)
			.Select(x => x.Overall)
			.ToListAsync(ct);

		return roster.Count > 0
			? (int)Math.Round(roster.Average())
			: gameClub.OverallRating;
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
		}

		if (!string.IsNullOrWhiteSpace(user?.AvatarUrl))
			return user.AvatarUrl;

		return string.Empty;
	}
}
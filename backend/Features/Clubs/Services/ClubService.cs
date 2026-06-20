using backend.Data;
using backend.Entities.Enums;
using backend.Entities.GameData;
using backend.Features.ActivityFeed.Services;
using backend.Features.Clubs.Dtos;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.Clubs.Services;

public class ClubService : IClubService
{
	private readonly AppDbContext _context;
	private readonly IActivityFeedService _activityFeedService;

	public ClubService(
		AppDbContext context,
		IActivityFeedService activityFeedService)
	{
		_context = context;
		_activityFeedService = activityFeedService;
	}

	public async Task<MyClubDto?> GetMyClubAsyncByAuth0Id(string auth0Id)
	{
		var user = await _context.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted &&
				x.IsActive);

		if (user is null)
			return null;

		return await GetMyClubAsync(user.UserId);
	}

	public async Task<MyClubDto?> GetMyClubAsync(Guid userId)
	{
		var user = await _context.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.UserId == userId &&
				!x.IsDeleted &&
				x.IsActive);

		if (user is null)
			return null;

		var season = await _context.Seasons
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.IsCurrent &&
				x.IsActive &&
				!x.IsDeleted);

		if (season is null)
			return null;

		var userClub = await _context.UserClubs
			.AsNoTracking()
			.Include(x => x.BaseGameClub)
			.FirstOrDefaultAsync(x =>
				x.UserId == user.UserId &&
				x.SeasonId == season.SeasonId &&
				!x.IsDeleted);

		if (userClub is null)
			return null;

		var baseClub = userClub.BaseGameClub;

		var matches = await _context.Matches
			.AsNoTracking()
			.Where(x =>
				!x.IsDeleted &&
				x.Status == MatchStatus.Finished &&
				(
					x.HomeUserId == user.UserId ||
					x.AwayUserId == user.UserId
				))
			.ToListAsync();

		var matchesPlayed = matches.Count;

		var wins = matches.Count(x =>
			(x.HomeUserId == user.UserId && x.HomeScore > x.AwayScore) ||
			(x.AwayUserId == user.UserId && x.AwayScore > x.HomeScore));

		var losses = matches.Count(x =>
			(x.HomeUserId == user.UserId && x.HomeScore < x.AwayScore) ||
			(x.AwayUserId == user.UserId && x.AwayScore < x.HomeScore));

		var draws = matches.Count(x => x.HomeScore == x.AwayScore);

		var goalsFor = matches.Sum(x =>
			x.HomeUserId == user.UserId
				? x.HomeScore
				: x.AwayScore);

		var goalsAgainst = matches.Sum(x =>
			x.HomeUserId == user.UserId
				? x.AwayScore
				: x.HomeScore);

		var goalDifference = goalsFor - goalsAgainst;

		var rating = wins * 3 + draws;

		var players = await _context.UserClubPlayers
			.AsNoTracking()
			.Include(x => x.GamePlayer)
			.Where(x =>
				x.UserClubId == userClub.UserClubId &&
				!x.IsDeleted &&
				!x.GamePlayer.IsDeleted &&
				x.GamePlayer.IsActive)
			.OrderByDescending(x => x.GamePlayer.Overall)
			.Select(x => new MyClubPlayerDto
			{
				PlayerId = x.GamePlayerId,
				Name = string.IsNullOrWhiteSpace(x.GamePlayer.Name)
					? x.GamePlayer.FullName
					: x.GamePlayer.Name,
				Position = x.GamePlayer.Position,
				Overall = x.GamePlayer.Overall,
				AvatarUrl = x.GamePlayer.ImageUrl,
				X = 50,
				Y = 50
			})
			.ToListAsync();

		SetFormationPositions(players);

		var activities = await _context.ActivityFeeds
			.AsNoTracking()
			.Where(x => x.UserId == user.UserId)
			.OrderByDescending(x => x.CreatedAtUTC)
			.Take(8)
			.Select(x => new MyClubActivityDto
			{
				Id = x.Id,
				Type = x.Type,
				Text = x.Text,
				Icon = GetActivityIcon(x.Type),
				CreatedAtUTC = x.CreatedAtUTC
			})
			.ToListAsync();

		return new MyClubDto
		{
			ClubId = userClub.UserClubId,
			Name = userClub.Name,

			Abbreviation = baseClub?.Code ?? string.Empty,
			LogoUrl = string.IsNullOrWhiteSpace(userClub.LogoUrl)
				? baseClub?.LogoUrl ?? string.Empty
				: userClub.LogoUrl,

			Description = string.Empty,
			Slogan = null,
			Region = baseClub?.Country ?? string.Empty,
			Division = baseClub?.League ?? string.Empty,

			Members = 1,

			MatchesPlayed = matchesPlayed,
			Wins = wins,
			Losses = losses,
			Draws = draws,
			GoalsFor = goalsFor,
			GoalsAgainst = goalsAgainst,
			GoalDifference = goalDifference,
			Rating = rating > 0 ? rating : baseClub?.OverallRating ?? 0,

			Players = players,
			Activities = activities
		};
	}

	public async Task<MyClubDto?> UpdateClubLogoAsync(string auth0Id, string logoUrl)
	{
		if (string.IsNullOrWhiteSpace(logoUrl))
			return null;

		var user = await _context.Users
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted &&
				x.IsActive);

		if (user is null)
			return null;

		var season = await _context.Seasons
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.IsCurrent &&
				x.IsActive &&
				!x.IsDeleted);

		if (season is null)
			return null;

		var userClub = await _context.UserClubs
			.FirstOrDefaultAsync(x =>
				x.UserId == user.UserId &&
				x.SeasonId == season.SeasonId &&
				!x.IsDeleted);

		if (userClub is null)
			return null;

		userClub.LogoUrl = logoUrl;

		await _context.SaveChangesAsync();

		await _activityFeedService.AddAsync(
			user.UserId,
			"LOGO_UPDATED",
			"Club logo updated"
		);

		return await GetMyClubAsync(user.UserId);
	}

	public async Task<MyClubDto?> UpdateMyClubLineupAsync(
		string auth0Id,
		UpdateMyClubLineupDto dto)
	{
		if (dto is null || string.IsNullOrWhiteSpace(dto.Formation))
			return null;

		if (dto.Players is null || dto.Players.Count != 11)
			return null;

		var user = await _context.Users
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted &&
				x.IsActive);

		if (user is null)
			return null;

		var season = await _context.Seasons
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.IsCurrent &&
				x.IsActive &&
				!x.IsDeleted);

		if (season is null)
			return null;

		var userClub = await _context.UserClubs
			.FirstOrDefaultAsync(x =>
				x.UserId == user.UserId &&
				x.SeasonId == season.SeasonId &&
				!x.IsDeleted);

		if (userClub is null)
			return null;

		var gamePlayerIds = dto.Players
			.Select(x => x.PlayerId)
			.ToList();

		var distinctGamePlayerIds = gamePlayerIds
			.Distinct()
			.ToList();

		if (distinctGamePlayerIds.Count != 11)
			return null;

		var userClubPlayers = await _context.UserClubPlayers
			.Where(x =>
				x.UserClubId == userClub.UserClubId &&
				distinctGamePlayerIds.Contains(x.GamePlayerId) &&
				!x.IsDeleted)
			.ToListAsync();

		if (userClubPlayers.Count != 11)
			return null;

		var userClubPlayerMap = userClubPlayers
			.ToDictionary(x => x.GamePlayerId, x => x.UserClubPlayerId);

		var lineup = await _context.ClubLineups
			.Include(x => x.Players)
			.FirstOrDefaultAsync(x =>
				x.UserClubId == userClub.UserClubId &&
				!x.IsDeleted);

		if (lineup is null)
		{
			lineup = new ClubLineup
			{
				ClubLineupId = Guid.NewGuid(),
				UserClubId = userClub.UserClubId,
				Formation = dto.Formation
			};

			_context.ClubLineups.Add(lineup);
		}
		else
		{
			lineup.Formation = dto.Formation;
			_context.ClubLineupPlayers.RemoveRange(lineup.Players);
		}

		lineup.Players = dto.Players
			.OrderBy(x => x.SlotIndex)
			.Select(x => new ClubLineupPlayer
			{
				ClubLineupPlayerId = Guid.NewGuid(),
				ClubLineupId = lineup.ClubLineupId,
				UserClubPlayerId = userClubPlayerMap[x.PlayerId],
				SlotIndex = x.SlotIndex,
				Position = x.Position ?? string.Empty
			})
			.ToList();

		await _context.SaveChangesAsync();

		await _activityFeedService.AddAsync(
			user.UserId,
			"LINEUP_UPDATED",
			$"Starting XI updated to {dto.Formation}"
		);

		return await GetMyClubAsync(user.UserId);
	}

	private static void SetFormationPositions(List<MyClubPlayerDto> players)
	{
		var positions = new[]
		{
			new { X = 18, Y = 15 },
			new { X = 47, Y = 8 },
			new { X = 74, Y = 15 },
			new { X = 28, Y = 42 },
			new { X = 48, Y = 38 },
			new { X = 68, Y = 42 },
			new { X = 12, Y = 67 },
			new { X = 34, Y = 70 },
			new { X = 60, Y = 70 },
			new { X = 82, Y = 67 },
			new { X = 48, Y = 88 }
		};

		for (var i = 0; i < players.Count && i < positions.Length; i++)
		{
			players[i].X = positions[i].X;
			players[i].Y = positions[i].Y;
		}
	}

	private static string GetActivityIcon(string type)
	{
		return type switch
		{
			"LOGO_UPDATED" => "fa-solid fa-image",
			"LINEUP_UPDATED" => "fa-solid fa-users",
			"FORMATION_CHANGED" => "fa-solid fa-chess-board",
			"MATCH_WON" => "fa-solid fa-trophy",
			"MATCH_LOST" => "fa-solid fa-circle-xmark",
			"MATCH_DRAW" => "fa-solid fa-handshake",
			"PLAYER_JOINED" => "fa-solid fa-user-plus",
			"PLAYER_LEFT" => "fa-solid fa-user-minus",
			"CLUB_CREATED" => "fa-solid fa-shield-halved",
			"TOURNAMENT_REGISTERED" => "fa-solid fa-flag",
			_ => "fa-solid fa-bolt"
		};
	}
}
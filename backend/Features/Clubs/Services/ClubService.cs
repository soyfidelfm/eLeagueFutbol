using backend.Data;
using backend.Entities.Enums;
using backend.Features.Clubs.Dtos;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.Clubs.Services;

public class ClubService : IClubService
{
	private readonly AppDbContext _context;

	public ClubService(AppDbContext context)
	{
		_context = context;
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

		if (user is null || user.GameClubId is null)
			return null;

		var gameClubId = user.GameClubId.Value;

		var gameClub = await _context.GameClubs
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.GameClubId == gameClubId &&
				!x.IsDeleted &&
				x.IsActive);

		if (gameClub is null)
			return null;

		var members = await _context.Users
			.AsNoTracking()
			.CountAsync(x =>
				x.UserId == userId &&
				!x.IsDeleted &&
				x.IsActive);

		var teamIds = await _context.Teams
			.AsNoTracking()
			.Where(x =>
				x.GameClubId == gameClubId &&
				!x.IsDeleted)
			.Select(x => x.TeamId)
			.ToListAsync();

		var matches = await _context.Matches
			.AsNoTracking()
			.Where(x =>
				!x.IsDeleted &&
				x.Status == MatchStatus.Finished &&
				(
					teamIds.Contains(x.HomeTeamId) ||
					teamIds.Contains(x.AwayTeamId)
				))
			.ToListAsync();

		var matchesPlayed = matches.Count;

		var wins = matches.Count(x =>
			(teamIds.Contains(x.HomeTeamId) && x.HomeScore > x.AwayScore) ||
			(teamIds.Contains(x.AwayTeamId) && x.AwayScore > x.HomeScore));

		var losses = matches.Count(x =>
			(teamIds.Contains(x.HomeTeamId) && x.HomeScore < x.AwayScore) ||
			(teamIds.Contains(x.AwayTeamId) && x.AwayScore < x.HomeScore));

		var draws = matches.Count(x =>
			x.HomeScore == x.AwayScore);

		var goalsFor = matches.Sum(x =>
			teamIds.Contains(x.HomeTeamId)
				? x.HomeScore
				: x.AwayScore);

		var goalsAgainst = matches.Sum(x =>
			teamIds.Contains(x.HomeTeamId)
				? x.AwayScore
				: x.HomeScore);

		var goalDifference = goalsFor - goalsAgainst;

		var rating = wins * 3 + draws;

		var players = await _context.GamePlayers
			.AsNoTracking()
			.Where(x =>
				x.GameClubId == gameClubId &&
				!x.IsDeleted &&
				x.IsActive)
			.OrderByDescending(x => x.Overall)
			.Take(11)
			.Select(x => new MyClubPlayerDto
			{
				PlayerId = x.GamePlayerId,
				Name = string.IsNullOrWhiteSpace(x.Name)
					? x.FullName
					: x.Name,
				Position = x.Position,
				Overall = x.Overall,
				AvatarUrl = x.ImageUrl,
				X = 50,
				Y = 50
			})
			.ToListAsync();

		SetFormationPositions(players);

		var clubUserIds = await _context.Users
			.AsNoTracking()
			.Where(x =>
				x.GameClubId == gameClubId &&
				!x.IsDeleted &&
				x.IsActive)
			.Select(x => x.UserId)
			.ToListAsync();

		var activities = await _context.ActivityFeeds
			.AsNoTracking()
			.Where(x => clubUserIds.Contains(x.UserId))
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
			ClubId = gameClub.GameClubId,

			Name = string.IsNullOrWhiteSpace(user.ClubName)
				? gameClub.Name
				: user.ClubName,

			Abbreviation = gameClub.Code,
			LogoUrl = gameClub.LogoUrl,
			Description = string.Empty,
			Slogan = null,
			Region = gameClub.Country,
			Division = gameClub.League,

			Members = members,

			MatchesPlayed = matchesPlayed,
			Wins = wins,
			Losses = losses,
			Draws = draws,
			GoalsFor = goalsFor,
			GoalsAgainst = goalsAgainst,
			GoalDifference = goalDifference,
			Rating = rating > 0 ? rating : gameClub.OverallRating,

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

		if (user is null || user.GameClubId is null)
			return null;

		var gameClub = await _context.GameClubs
			.FirstOrDefaultAsync(x =>
				x.GameClubId == user.GameClubId.Value &&
				!x.IsDeleted &&
				x.IsActive);

		if (gameClub is null)
			return null;

		gameClub.LogoUrl = logoUrl;
		gameClub.UpdatedAtUTC = DateTime.UtcNow;

		await _context.SaveChangesAsync();

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
			"MatchWon" => "fa-solid fa-trophy",
			"MatchLost" => "fa-solid fa-circle-xmark",
			"MatchDraw" => "fa-solid fa-handshake",
			"PlayerJoined" => "fa-solid fa-user-plus",
			"PlayerLeft" => "fa-solid fa-user-minus",
			"ClubCreated" => "fa-solid fa-shield-halved",
			"TournamentRegistered" => "fa-solid fa-flag",
			_ => "fa-solid fa-bolt"
		};
	}
}
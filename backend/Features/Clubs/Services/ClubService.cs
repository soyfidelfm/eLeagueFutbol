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

		var formation = "4-3-3 Control";

		var allPlayers = await _context.UserClubPlayers
			.AsNoTracking()
			.Include(x => x.GamePlayer)
				.ThenInclude(x => x.GameClub)
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
				SlotIndex = 0,
				Nation = x.GamePlayer.Nation,
				League = x.GamePlayer.GameClub != null
					? x.GamePlayer.GameClub.League
					: null,
				Chemistry = 0,
				X = 50,
				Y = 50
			})
			.ToListAsync();

		var lineup = await _context.ClubLineups
			.AsNoTracking()
			.Include(x => x.Players)
				.ThenInclude(x => x.UserClubPlayer)
					.ThenInclude(x => x.GamePlayer)
						.ThenInclude(x => x.GameClub)
			.FirstOrDefaultAsync(x =>
				x.UserClubId == userClub.UserClubId &&
				!x.IsDeleted);

		List<MyClubPlayerDto> startingPlayers;

		if (lineup is not null && lineup.Players.Count == 11)
		{
			formation = lineup.Formation;

			startingPlayers = lineup.Players
				.OrderBy(x => x.SlotIndex)
				.Select(x => new MyClubPlayerDto
				{
					PlayerId = x.UserClubPlayer.GamePlayerId,
					Name = string.IsNullOrWhiteSpace(x.UserClubPlayer.GamePlayer.Name)
						? x.UserClubPlayer.GamePlayer.FullName
						: x.UserClubPlayer.GamePlayer.Name,
					Position = x.Position,
					Overall = x.UserClubPlayer.GamePlayer.Overall,
					AvatarUrl = x.UserClubPlayer.GamePlayer.ImageUrl,
					SlotIndex = x.SlotIndex,
					Nation = x.UserClubPlayer.GamePlayer.Nation,
					League = x.UserClubPlayer.GamePlayer.GameClub != null
						? x.UserClubPlayer.GamePlayer.GameClub.League
						: null,
					Chemistry = 0,
					X = 50,
					Y = 50
				})
				.ToList();
		}
		else
		{
			startingPlayers = allPlayers
				.Take(11)
				.Select((x, index) => new MyClubPlayerDto
				{
					PlayerId = x.PlayerId,
					Name = x.Name,
					Position = x.Position,
					Overall = x.Overall,
					AvatarUrl = x.AvatarUrl,
					SlotIndex = index,
					Nation = x.Nation,
					League = x.League,
					Chemistry = 0,
					X = 50,
					Y = 50
				})
				.ToList();
		}

		SetFormationPositions(startingPlayers, formation);
		CalculateChemistry(startingPlayers, formation);

		var chemistry = startingPlayers.Sum(x => x.Chemistry);

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
			Formation = formation,

			Abbreviation = baseClub?.Code ?? string.Empty,
			LogoUrl = string.IsNullOrWhiteSpace(userClub.LogoUrl)
				? string.Empty
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

			Chemistry = chemistry,

			Players = allPlayers,
			StartingPlayers = startingPlayers,

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
			.AsNoTracking()
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

			await _context.SaveChangesAsync();
		}
		else
		{
			lineup.Formation = dto.Formation;

			await _context.SaveChangesAsync();

			await _context.ClubLineupPlayers
				.Where(x => x.ClubLineupId == lineup.ClubLineupId)
				.ExecuteDeleteAsync();
		}

		var newPlayers = dto.Players
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

		_context.ClubLineupPlayers.AddRange(newPlayers);

		await _context.SaveChangesAsync();

		await _activityFeedService.AddAsync(
			user.UserId,
			"LINEUP_UPDATED",
			$"Starting XI updated to {dto.Formation}"
		);

		return await GetMyClubAsync(user.UserId);
	}

	private sealed class FormationPoint
	{
		public int X { get; set; }
		public int Y { get; set; }
	}

	private static void SetFormationPositions(
		List<MyClubPlayerDto> players,
		string formation)
	{
		List<FormationPoint> positions = formation switch
		{
			"4-4-2 Flat" => new List<FormationPoint>
		{
			new FormationPoint { X = 35, Y = 12 },
			new FormationPoint { X = 65, Y = 12 },

			new FormationPoint { X = 15, Y = 38 },
			new FormationPoint { X = 38, Y = 40 },
			new FormationPoint { X = 62, Y = 40 },
			new FormationPoint { X = 85, Y = 38 },

			new FormationPoint { X = 15, Y = 68 },
			new FormationPoint { X = 38, Y = 72 },
			new FormationPoint { X = 62, Y = 72 },
			new FormationPoint { X = 85, Y = 68 },

			new FormationPoint { X = 50, Y = 90 }
		},

			"4-1-2-1-2 Narrow" => new List<FormationPoint>
		{
			new FormationPoint { X = 35, Y = 12 },
			new FormationPoint { X = 65, Y = 12 },

			new FormationPoint { X = 50, Y = 30 },

			new FormationPoint { X = 35, Y = 45 },
			new FormationPoint { X = 65, Y = 45 },

			new FormationPoint { X = 50, Y = 58 },

			new FormationPoint { X = 15, Y = 72 },
			new FormationPoint { X = 38, Y = 76 },
			new FormationPoint { X = 62, Y = 76 },
			new FormationPoint { X = 85, Y = 72 },

			new FormationPoint { X = 50, Y = 92 }
		},

			_ => new List<FormationPoint>
		{
			new FormationPoint { X = 18, Y = 15 },
			new FormationPoint { X = 47, Y = 8 },
			new FormationPoint { X = 74, Y = 15 },

			new FormationPoint { X = 28, Y = 42 },
			new FormationPoint { X = 48, Y = 38 },
			new FormationPoint { X = 68, Y = 42 },

			new FormationPoint { X = 12, Y = 67 },
			new FormationPoint { X = 34, Y = 70 },
			new FormationPoint { X = 60, Y = 70 },
			new FormationPoint { X = 82, Y = 67 },

			new FormationPoint { X = 48, Y = 88 }
		}
		};

		for (var i = 0; i < players.Count && i < positions.Count; i++)
		{
			players[i].X = positions[i].X;
			players[i].Y = positions[i].Y;
		}
	}

	private static void CalculateChemistry(
		List<MyClubPlayerDto> players,
		string formation)
	{
		var slotPositions = GetFormationSlotPositions(formation);

		foreach (var player in players)
		{
			var playerChemistry = 0;

			if (slotPositions.TryGetValue(player.SlotIndex, out var expectedPosition) &&
				IsCorrectPosition(player.Position, expectedPosition))
			{
				playerChemistry++;
			}

			if (!string.IsNullOrWhiteSpace(player.League) &&
				players.Count(x => x.League == player.League) >= 2)
			{
				playerChemistry++;
			}

			if (!string.IsNullOrWhiteSpace(player.Nation) &&
				players.Count(x => x.Nation == player.Nation) >= 2)
			{
				playerChemistry++;
			}

			player.Chemistry = Math.Min(playerChemistry, 3);
		}
	}

	private static Dictionary<int, string> GetFormationSlotPositions(string formation)
	{
		return formation switch
		{
			"4-4-2 Flat" => new Dictionary<int, string>
			{
				{ 0, "ST" },
				{ 1, "ST" },
				{ 2, "LM" },
				{ 3, "CM" },
				{ 4, "CM" },
				{ 5, "RM" },
				{ 6, "LB" },
				{ 7, "CB" },
				{ 8, "CB" },
				{ 9, "RB" },
				{ 10, "GK" }
			},

			"4-1-2-1-2 Narrow" => new Dictionary<int, string>
			{
				{ 0, "ST" },
				{ 1, "ST" },
				{ 2, "CAM" },
				{ 3, "CM" },
				{ 4, "CM" },
				{ 5, "CDM" },
				{ 6, "LB" },
				{ 7, "CB" },
				{ 8, "CB" },
				{ 9, "RB" },
				{ 10, "GK" }
			},

			_ => new Dictionary<int, string>
			{
				{ 0, "LW" },
				{ 1, "ST" },
				{ 2, "RW" },
				{ 3, "CM" },
				{ 4, "CM" },
				{ 5, "CM" },
				{ 6, "LB" },
				{ 7, "CB" },
				{ 8, "CB" },
				{ 9, "RB" },
				{ 10, "GK" }
			}
		};
	}

	private static bool IsCorrectPosition(string? playerPositions, string expectedPosition)
	{
		if (string.IsNullOrWhiteSpace(playerPositions))
			return false;

		var positions = playerPositions
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		return positions.Any(x =>
			string.Equals(x, expectedPosition, StringComparison.OrdinalIgnoreCase));
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
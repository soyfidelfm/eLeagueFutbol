using backend.Data;
using backend.Entities.Enums;
using backend.Features.Rankings.DTOs;
using backend.Features.Rankings.Dtos;
using backend.Features.Rankings.Services;
using Microsoft.EntityFrameworkCore;
using backend.Entities.Tournaments;

namespace backend.Application.Rankings.Services;

public sealed class RankingService : IRankingService
{
	private readonly AppDbContext _context;

	public RankingService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<RankingsResponseDto> GetRankingsAsync(Guid? seasonId)
	{
		var seasons = await _context.Seasons
			.Where(x => !x.IsDeleted && x.IsActive)
			.OrderByDescending(x => x.StartDate)
			.Select(x => new RankingSeasonDto
			{
				SeasonId = x.SeasonId,
				Name = x.Name,
				IsCurrent = x.IsCurrent
			})
			.ToListAsync();

		var selectedSeason = seasonId.HasValue
			? seasons.FirstOrDefault(x => x.SeasonId == seasonId.Value)
			: seasons.FirstOrDefault(x => x.IsCurrent) ?? seasons.FirstOrDefault();

		if (selectedSeason is null)
		{
			return new RankingsResponseDto();
		}

		var matches = await _context.Matches
			.Include(x => x.Tournament)
			.Include(x => x.HomeGameClub)
			.Include(x => x.AwayGameClub)
			.Where(x =>
				!x.IsDeleted &&
				x.Status == MatchStatus.Finished &&
				x.Tournament.SeasonId == selectedSeason.SeasonId)
			.ToListAsync();

		var playerRankings = await BuildPlayerRankingsAsync(selectedSeason.SeasonId);
		var clubRankings = await BuildClubRankingsAsync(selectedSeason.SeasonId, matches);

		return new RankingsResponseDto
		{
			SeasonId = selectedSeason.SeasonId,
			SeasonName = selectedSeason.Name,
			Seasons = seasons,
			PlayerRankings = playerRankings,
			ClubRankings = clubRankings
		};
	}

	private async Task<List<PlayerRankingDto>> BuildPlayerRankingsAsync(Guid seasonId)
	{
		var registrations = await _context.SeasonRegistrations
			.Include(x => x.User)
			.Include(x => x.GameClub)
			.Where(x =>
				x.SeasonId == seasonId)
			.ToListAsync();

		var table = registrations
			.GroupBy(x => x.UserId)
			.ToDictionary(
				g => g.Key,
				g =>
				{
					var registration = g.First();

					return new PlayerRankingDto
					{
						UserId = registration.UserId,
						PlayerName = registration.User.DisplayName,
						AvatarUrl = registration.User.AvatarUrl ?? string.Empty,

						ClubId = registration.GameClubId,
						ClubName = registration.User.ClubName ?? registration.GameClub.Name,
						ClubLogoUrl = registration.GameClub.LogoUrl ?? string.Empty,

						Points = 0,
						Wins = 0,
						Losses = 0,
						GoalsFor = 0,
						GoalsAgainst = 0,
						WinRate = 0,
						Form = []
					};
				});

		var stats = await _context.MatchPlayerStats
			.Include(x => x.Match)
				.ThenInclude(x => x.Tournament)
			.Where(x =>
				!x.IsDeleted &&
				x.Match.Status == MatchStatus.Finished &&
				x.Match.Tournament.SeasonId == seasonId)
			.GroupBy(x => x.UserId)
			.Select(g => new
			{
				UserId = g.Key,
				Goals = g.Sum(x => x.Goals),
				Assists = g.Sum(x => x.Assists)
			})
			.ToListAsync();

		foreach (var stat in stats)
		{
			if (!table.TryGetValue(stat.UserId, out var player))
			{
				continue;
			}

			player.GoalsFor = stat.Goals;
			player.Points = stat.Goals * 10 + stat.Assists * 5;
		}

		var rankings = table.Values
			.OrderByDescending(x => x.Points)
			.ThenByDescending(x => x.GoalsFor)
			.ThenBy(x => x.PlayerName)
			.ToList();

		for (var i = 0; i < rankings.Count; i++)
		{
			rankings[i].Rank = i + 1;
		}

		return rankings;
	}

	private async Task<List<ClubRankingDto>> BuildClubRankingsAsync(
		Guid seasonId,
		List<Match> matches)
	{
		var registrations = await _context.SeasonRegistrations
			.Include(x => x.GameClub)
			.Where(x =>
				x.SeasonId == seasonId)
			.ToListAsync();

		var table = registrations
			.GroupBy(x => x.GameClubId)
			.ToDictionary(
				g => g.Key,
				g =>
				{
					var registration = g.First();

					return new ClubRankingDto
					{
						ClubId = registration.GameClubId,
						ClubName = registration.User.ClubName ?? registration.GameClub.Name,
						ClubLogoUrl = registration.GameClub.LogoUrl ?? string.Empty,

						Points = 0,
						Wins = 0,
						Draws = 0,
						Losses = 0,
						GoalsFor = 0,
						GoalsAgainst = 0,
						GoalDifference = 0,
						WinRate = 0,
						Form = []
					};
				});

		foreach (var match in matches)
		{
			AddClubIfMissing(table, match.HomeGameClubId, match.HomeGameClub.Name, match.HomeGameClub.LogoUrl);
			AddClubIfMissing(table, match.AwayGameClubId, match.AwayGameClub.Name, match.AwayGameClub.LogoUrl);

			var home = table[match.HomeGameClubId];
			var away = table[match.AwayGameClubId];

			home.GoalsFor += match.HomeScore;
			home.GoalsAgainst += match.AwayScore;

			away.GoalsFor += match.AwayScore;
			away.GoalsAgainst += match.HomeScore;

			if (match.HomeScore > match.AwayScore)
			{
				home.Wins++;
				home.Points += 3;
				home.Form.Add("W");

				away.Losses++;
				away.Form.Add("L");
			}
			else if (match.HomeScore < match.AwayScore)
			{
				away.Wins++;
				away.Points += 3;
				away.Form.Add("W");

				home.Losses++;
				home.Form.Add("L");
			}
			else
			{
				home.Draws++;
				away.Draws++;

				home.Points++;
				away.Points++;

				home.Form.Add("D");
				away.Form.Add("D");
			}
		}

		var rankings = table.Values
			.Select(x =>
			{
				var games = x.Wins + x.Draws + x.Losses;

				x.GoalDifference = x.GoalsFor - x.GoalsAgainst;
				x.WinRate = games == 0 ? 0 : Math.Round((decimal)x.Wins / games * 100, 2);
				x.Form = x.Form.TakeLast(5).ToList();

				return x;
			})
			.OrderByDescending(x => x.Points)
			.ThenByDescending(x => x.GoalDifference)
			.ThenByDescending(x => x.GoalsFor)
			.ThenBy(x => x.ClubName)
			.ToList();

		for (var i = 0; i < rankings.Count; i++)
		{
			rankings[i].Rank = i + 1;
		}

		return rankings;
	}

	private static void AddClubIfMissing(
		Dictionary<Guid, ClubRankingDto> table,
		Guid clubId,
		string clubName,
		string? logoUrl)
	{
		if (table.ContainsKey(clubId))
		{
			return;
		}

		table[clubId] = new ClubRankingDto
		{
			ClubId = clubId,
			ClubName = clubName,
			ClubLogoUrl = logoUrl ?? string.Empty,
			Form = []
		};
	}
}
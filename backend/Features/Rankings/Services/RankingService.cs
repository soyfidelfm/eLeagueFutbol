using backend.Data;
using backend.Entities.Clubs;
using backend.Entities.Enums;
using backend.Features.Rankings.DTOs;
using backend.Features.Rankings.Dtos;
using Microsoft.EntityFrameworkCore;
using backend.Entities.Tournaments;

namespace backend.Features.Rankings.Services;

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
			return new RankingsResponseDto();

		var matches = await _context.Matches
			.Include(x => x.Tournament)
			.Include(x => x.HomeUser)
			.Include(x => x.AwayUser)
			.Include(x => x.HomeGameClub)
			.Include(x => x.AwayGameClub)
			.Where(x =>
				!x.IsDeleted &&
				x.Status == MatchStatus.Finished &&
				x.Tournament.SeasonId == selectedSeason.SeasonId)
			.ToListAsync();

		var userClubs = await _context.UserClubs
			.AsNoTracking()
			.Include(x => x.User)
			.Include(x => x.BaseGameClub)
			.Where(x =>
				x.SeasonId == selectedSeason.SeasonId &&
				!x.IsDeleted)
			.ToListAsync();

		var userClubMap = userClubs
			.GroupBy(x => x.UserId)
			.ToDictionary(g => g.Key, g => g.First());

		var playerRankings = await BuildPlayerRankingsAsync(
			selectedSeason.SeasonId,
			userClubs);

		var clubRankings = BuildClubRankings(
			matches,
			userClubs,
			userClubMap);

		return new RankingsResponseDto
		{
			SeasonId = selectedSeason.SeasonId,
			SeasonName = selectedSeason.Name,
			Seasons = seasons,
			PlayerRankings = playerRankings,
			ClubRankings = clubRankings
		};
	}

	private async Task<List<PlayerRankingDto>> BuildPlayerRankingsAsync(
		Guid seasonId,
		List<UserClub> userClubs)
	{
		var table = userClubs
			.GroupBy(x => x.UserId)
			.ToDictionary(
				g => g.Key,
				g =>
				{
					var club = g.First();

					return new PlayerRankingDto
					{
						UserId = club.UserId,
						PlayerName = string.IsNullOrWhiteSpace(club.User.DisplayName)
							? club.User.Email
							: club.User.DisplayName,
						AvatarUrl = string.IsNullOrWhiteSpace(club.User.AvatarUrl)
							? string.Empty
							: club.User.AvatarUrl,

						ClubId = club.UserClubId,
						ClubName = club.Name,
						ClubLogoUrl = GetClubLogo(club),

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
				continue;

			player.Points += stat.Goals * 10 + stat.Assists * 5;
		}

		var finishedMatches = await _context.Matches
			.Include(x => x.Tournament)
			.Where(x =>
				!x.IsDeleted &&
				x.Status == MatchStatus.Finished &&
				x.Tournament.SeasonId == seasonId)
			.OrderBy(x => x.MatchDate)
			.ToListAsync();

		foreach (var match in finishedMatches)
		{
			if (!table.TryGetValue(match.HomeUserId, out var home))
				continue;

			if (!table.TryGetValue(match.AwayUserId, out var away))
				continue;

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
				home.Form.Add("D");
				away.Form.Add("D");
			}
		}

		var rankings = table.Values
			.Select(x =>
			{
				var games = x.Wins + x.Losses;

				x.WinRate = games == 0
					? 0
					: Math.Round((decimal)x.Wins / games * 100, 2);

				x.Form = x.Form.TakeLast(5).ToList();

				return x;
			})
			.OrderByDescending(x => x.Points)
			.ThenByDescending(x => x.GoalsFor)
			.ThenBy(x => x.PlayerName)
			.ToList();

		for (var i = 0; i < rankings.Count; i++)
			rankings[i].Rank = i + 1;

		return rankings;
	}

	private static List<ClubRankingDto> BuildClubRankings(
		List<Match> matches,
		List<UserClub> userClubs,
		Dictionary<Guid, UserClub> userClubMap)
	{
		var table = userClubs
			.GroupBy(x => x.UserClubId)
			.ToDictionary(
				g => g.Key,
				g =>
				{
					var club = g.First();

					return new ClubRankingDto
					{
						ClubId = club.UserClubId,
						ClubName = club.Name,
						ClubLogoUrl = GetClubLogo(club),

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

		foreach (var match in matches.OrderBy(x => x.MatchDate))
		{
			if (!userClubMap.TryGetValue(match.HomeUserId, out var homeClub))
				continue;

			if (!userClubMap.TryGetValue(match.AwayUserId, out var awayClub))
				continue;

			var home = table[homeClub.UserClubId];
			var away = table[awayClub.UserClubId];

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
				x.WinRate = games == 0
					? 0
					: Math.Round((decimal)x.Wins / games * 100, 2);

				x.Form = x.Form.TakeLast(5).ToList();

				return x;
			})
			.OrderByDescending(x => x.Points)
			.ThenByDescending(x => x.GoalDifference)
			.ThenByDescending(x => x.GoalsFor)
			.ThenBy(x => x.ClubName)
			.ToList();

		for (var i = 0; i < rankings.Count; i++)
			rankings[i].Rank = i + 1;

		return rankings;
	}

	private static string GetClubLogo(UserClub club)
	{
		return string.IsNullOrWhiteSpace(club.LogoUrl)
			? string.Empty
			: club.LogoUrl;
	}
}
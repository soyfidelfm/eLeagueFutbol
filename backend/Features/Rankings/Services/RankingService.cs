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
		var clubsByUser = userClubs
			.GroupBy(x => x.UserId)
			.ToDictionary(g => g.Key, g => g.First());

		var finishedMatches = await _context.Matches
			.AsNoTracking()
			.Where(x =>
				!x.IsDeleted &&
				x.Status == MatchStatus.Finished &&
				x.Tournament.SeasonId == seasonId)
			.ToListAsync();

		// League points only (3/1/0). Goals/assists are separate columns, not points.
		var lines = LeaguePoints.Build(
			finishedMatches,
			userId => clubsByUser.ContainsKey(userId) ? userId : null);

		var stats = await _context.MatchPlayerStats
			.AsNoTracking()
			.Where(x =>
				!x.IsDeleted &&
				x.Match.Status == MatchStatus.Finished &&
				!x.Match.IsDeleted &&
				x.Match.Tournament.SeasonId == seasonId)
			.GroupBy(x => x.UserId)
			.Select(g => new
			{
				UserId = g.Key,
				Goals = g.Sum(x => x.Goals),
				Assists = g.Sum(x => x.Assists)
			})
			.ToDictionaryAsync(x => x.UserId);

		var rankings = clubsByUser.Values
			.Select(club =>
			{
				lines.TryGetValue(club.UserId, out var line);
				line ??= new LeaguePoints.Line();
				stats.TryGetValue(club.UserId, out var stat);

				return new PlayerRankingDto
				{
					UserId = club.UserId,
					PlayerName = string.IsNullOrWhiteSpace(club.User.DisplayName)
						? club.User.Email
						: club.User.DisplayName,
					AvatarUrl = club.User.AvatarUrl ?? string.Empty,

					ClubId = club.UserClubId,
					ClubName = club.Name,
					ClubLogoUrl = GetClubLogo(club),

					Points = line.Points,
					Wins = line.Wins,
					Draws = line.Draws,
					Losses = line.Losses,
					Played = line.Played,
					GoalsFor = line.GoalsFor,
					GoalsAgainst = line.GoalsAgainst,
					GoalDifference = line.GoalDifference,
					Goals = stat?.Goals ?? 0,
					Assists = stat?.Assists ?? 0,
					WinRate = line.WinRate,
					Form = line.LastFive
				};
			})
			.OrderByDescending(x => x.Points)
			.ThenByDescending(x => x.GoalDifference)
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
		var lines = LeaguePoints.Build(
			matches,
			userId => userClubMap.TryGetValue(userId, out var c) ? c.UserClubId : null);

		var rankings = userClubs
			.GroupBy(x => x.UserClubId)
			.Select(g =>
			{
				var club = g.First();
				lines.TryGetValue(club.UserClubId, out var line);
				line ??= new LeaguePoints.Line();

				return new ClubRankingDto
				{
					ClubId = club.UserClubId,
					ClubName = club.Name,
					ClubLogoUrl = GetClubLogo(club),

					Points = line.Points,
					Wins = line.Wins,
					Draws = line.Draws,
					Losses = line.Losses,
					Played = line.Played,
					GoalsFor = line.GoalsFor,
					GoalsAgainst = line.GoalsAgainst,
					GoalDifference = line.GoalDifference,
					WinRate = line.WinRate,
					Form = line.LastFive
				};
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
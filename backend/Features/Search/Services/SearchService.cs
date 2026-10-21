using System.Text.RegularExpressions;
using backend.Data;
using backend.Entities.Enums;
using backend.Features.Search.Dtos;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.Search.Services;

/// <summary>
/// Global search across players (users), clubs and matches.
/// Clubs and matches are scoped to the current season (fallback: latest season).
/// </summary>
public partial class SearchService : ISearchService
{
	private const int MaxPerType = 5;
	private const int MinLength = 2;

	private readonly AppDbContext _context;

	public SearchService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<GlobalSearchResultDto> SearchAsync(string auth0Id, string? query)
	{
		var result = new GlobalSearchResultDto();
		var term = (query ?? string.Empty).Trim().ToLower();

		if (term.Length < MinLength || term.Length > 100)
			return result;

		var meId = await _context.Users
			.AsNoTracking()
			.Where(x => x.Auth0Id == auth0Id && !x.IsDeleted)
			.Select(x => (Guid?)x.UserId)
			.FirstOrDefaultAsync();

		if (meId is null)
			return result;

		var seasonId = await _context.Seasons
			.AsNoTracking()
			.Where(x => !x.IsDeleted)
			.OrderByDescending(x => x.IsCurrent)
			.ThenByDescending(x => x.StartDate)
			.Select(x => (Guid?)x.SeasonId)
			.FirstOrDefaultAsync();

		// Players / users
		result.Players = await _context.Users
			.AsNoTracking()
			.Where(x =>
				!x.IsDeleted &&
				x.IsActive &&
				(x.DisplayName.ToLower().Contains(term) ||
				 x.GamerTag.ToLower().Contains(term)))
			.OrderBy(x => x.DisplayName)
			.Take(MaxPerType)
			.Select(x => new SearchPlayerDto
			{
				UserId = x.UserId,
				DisplayName = x.DisplayName,
				GamerTag = x.GamerTag,
				AvatarUrl = x.AvatarUrl,
				ClubName = x.ClubName,
				IsMe = x.UserId == meId.Value
			})
			.ToListAsync();

		if (seasonId is null)
			return result;

		// Clubs (user clubs of the season)
		result.Clubs = await _context.UserClubs
			.AsNoTracking()
			.Where(x =>
				!x.IsDeleted &&
				x.SeasonId == seasonId.Value &&
				x.Name.ToLower().Contains(term))
			.OrderBy(x => x.Name)
			.Take(MaxPerType)
			.Select(x => new SearchClubDto
			{
				ClubId = x.UserClubId,
				SeasonId = x.SeasonId,
				Name = x.Name,
				LogoUrl = x.LogoUrl,
				OwnerName = x.User.DisplayName,
				IsMine = x.UserId == meId.Value
			})
			.ToListAsync();

		// Matches: by team (club / user / game club) name or week number.
		var week = ParseWeek(term);

		var clubUserIds = await _context.UserClubs
			.AsNoTracking()
			.Where(x =>
				!x.IsDeleted &&
				x.SeasonId == seasonId.Value &&
				x.Name.ToLower().Contains(term))
			.Select(x => x.UserId)
			.ToListAsync();

		var clubNames = await _context.UserClubs
			.AsNoTracking()
			.Where(x => !x.IsDeleted && x.SeasonId == seasonId.Value)
			.Select(x => new { x.UserId, x.Name })
			.ToListAsync();

		var clubNameByUser = clubNames
			.GroupBy(x => x.UserId)
			.ToDictionary(g => g.Key, g => g.First().Name);

		var matches = await _context.Matches
			.AsNoTracking()
			.Where(x =>
				!x.IsDeleted &&
				x.Tournament.SeasonId == seasonId.Value &&
				(
					(week != null && x.WeekNumber == week) ||
					clubUserIds.Contains(x.HomeUserId) ||
					clubUserIds.Contains(x.AwayUserId) ||
					x.HomeUser.DisplayName.ToLower().Contains(term) ||
					x.AwayUser.DisplayName.ToLower().Contains(term) ||
					x.HomeUser.GamerTag.ToLower().Contains(term) ||
					x.AwayUser.GamerTag.ToLower().Contains(term) ||
					x.HomeGameClub.Name.ToLower().Contains(term) ||
					x.AwayGameClub.Name.ToLower().Contains(term)
				))
			.OrderBy(x => x.WeekNumber)
			.ThenBy(x => x.MatchDate)
			.Take(MaxPerType)
			.Select(x => new
			{
				x.MatchId,
				x.WeekNumber,
				x.HomeUserId,
				x.AwayUserId,
				HomeFallback = x.HomeGameClub.Name,
				AwayFallback = x.AwayGameClub.Name,
				x.HomeScore,
				x.AwayScore,
				x.Status,
				x.MatchDate
			})
			.ToListAsync();

		result.Matches = matches
			.Select(x => new SearchMatchDto
			{
				MatchId = x.MatchId,
				SeasonId = seasonId.Value,
				WeekNumber = x.WeekNumber,
				HomeName = clubNameByUser.GetValueOrDefault(x.HomeUserId) ?? x.HomeFallback,
				AwayName = clubNameByUser.GetValueOrDefault(x.AwayUserId) ?? x.AwayFallback,
				HomeScore = x.HomeScore,
				AwayScore = x.AwayScore,
				Status = x.Status.ToString(),
				MatchDate = x.MatchDate
			})
			.ToList();

		return result;
	}

	/// <summary>"3", "w3", "week 3", "semana 3", "jornada 3", "md3" → 3.</summary>
	private static int? ParseWeek(string term)
	{
		var m = WeekRegex().Match(term);
		return m.Success && int.TryParse(m.Groups[1].Value, out var n) && n > 0 && n < 1000 ? n : null;
	}

	[GeneratedRegex(@"^(?:w|wk|week|md|matchday|semana|jornada|j)?\s*#?\s*(\d{1,3})$")]
	private static partial Regex WeekRegex();
}

using backend.Data;
using backend.Entities.Enums;
using backend.Features.Rankings.Services;
using backend.Features.Sidebar.Dtos;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.Sidebar.Services;

public class SidebarService : ISidebarService
{
	private readonly AppDbContext _context;
	private readonly IRankingService _rankingService;

	public SidebarService(AppDbContext context, IRankingService rankingService)
	{
		_context = context;
		_rankingService = rankingService;
	}

	public async Task<SidebarDto> GetAsync(string? auth0Id = null)
	{
		var nowUtc = DateTime.UtcNow;

		var currentSeason = await _context.Seasons
			.AsNoTracking()
			.Where(x =>
				!x.IsDeleted &&
				x.IsActive &&
				x.IsCurrent)
			.OrderByDescending(x => x.StartDate)
			.FirstOrDefaultAsync();

		SidebarSeasonDto? seasonDto = null;

		if (currentSeason is not null)
		{
			var totalClubs = await _context.SeasonRegistrations
				.AsNoTracking()
				.CountAsync(x =>
					x.SeasonId == currentSeason.SeasonId);

			var totalMatches = totalClubs * (totalClubs - 1);

			seasonDto = new SidebarSeasonDto
			{
				Label = "CURRENT SEASON",
				Name = currentSeason.Name,
				TotalClubs = totalClubs,
				TotalMatches = totalMatches,
				StartsAtUtc = currentSeason.StartDate
			};
		}

		var nextCup = await _context.Tournaments
			.AsNoTracking()
			.Where(x =>
				!x.IsDeleted &&
				x.StartDate > nowUtc)
			.OrderBy(x => x.StartDate)
			.FirstOrDefaultAsync();

		SidebarCupDto? cupDto = null;

		if (nextCup is not null)
		{
			cupDto = new SidebarCupDto
			{
				Name = nextCup.Name,
				StartsAtUtc = nextCup.StartDate
			};
		}

		var dto = new SidebarDto
		{
			Season = seasonDto,
			Cup = cupDto
		};

		if (currentSeason is not null)
			await FillPersonalAsync(dto, currentSeason.SeasonId, auth0Id);

		return dto;
	}

	private async Task FillPersonalAsync(SidebarDto dto, Guid seasonId, string? auth0Id)
	{
		// Championship top scorer (finished matches of the season).
		var scorers = await _context.MatchPlayerStats
			.AsNoTracking()
			.Where(x =>
				!x.IsDeleted &&
				x.Goals > 0 &&
				x.Match.Status == MatchStatus.Finished &&
				!x.Match.IsDeleted &&
				x.Match.Tournament.SeasonId == seasonId)
			.GroupBy(x => new { x.UserClubId, x.PlayerName, ClubName = x.UserClub.Name })
			.Select(g => new
			{
				g.Key.UserClubId,
				g.Key.PlayerName,
				g.Key.ClubName,
				Goals = g.Sum(x => x.Goals)
			})
			.OrderByDescending(x => x.Goals)
			.ThenBy(x => x.PlayerName)
			.ToListAsync();

		var leader = scorers.FirstOrDefault();
		if (leader is not null)
		{
			dto.LeagueTopScorer = new SidebarScorerDto
			{
				PlayerName = leader.PlayerName,
				ClubName = leader.ClubName,
				Goals = leader.Goals
			};
		}

		// Championship top assist provider (same source: MatchPlayerStats).
		var topAssist = await _context.MatchPlayerStats
			.AsNoTracking()
			.Where(x =>
				!x.IsDeleted &&
				x.Assists > 0 &&
				x.Match.Status == MatchStatus.Finished &&
				!x.Match.IsDeleted &&
				x.Match.Tournament.SeasonId == seasonId)
			.GroupBy(x => new { x.UserClubId, x.PlayerName, ClubName = x.UserClub.Name })
			.Select(g => new
			{
				g.Key.PlayerName,
				g.Key.ClubName,
				Assists = g.Sum(x => x.Assists)
			})
			.OrderByDescending(x => x.Assists)
			.ThenBy(x => x.PlayerName)
			.FirstOrDefaultAsync();

		if (topAssist is not null)
		{
			dto.LeagueTopAssist = new SidebarAssistDto
			{
				PlayerName = topAssist.PlayerName,
				ClubName = topAssist.ClubName,
				Assists = topAssist.Assists
			};
		}

		if (string.IsNullOrWhiteSpace(auth0Id))
			return;

		var user = await _context.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Auth0Id == auth0Id && x.IsActive && !x.IsDeleted);

		if (user is null)
			return;

		var myClub = await _context.UserClubs
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.UserId == user.UserId &&
				x.SeasonId == seasonId &&
				!x.IsDeleted);

		if (myClub is not null)
		{
			var mine = scorers.FirstOrDefault(x => x.UserClubId == myClub.UserClubId);
			if (mine is not null)
			{
				dto.TeamTopScorer = new SidebarScorerDto
				{
					PlayerName = mine.PlayerName,
					ClubName = mine.ClubName,
					Goals = mine.Goals
				};
			}

			// Table position (same ordering as /api/rankings club table).
			var rankings = await _rankingService.GetRankingsAsync(seasonId);
			var row = rankings.ClubRankings.FirstOrDefault(x => x.ClubId == myClub.UserClubId);
			if (row is not null)
			{
				dto.Standing = new SidebarStandingDto
				{
					Position = row.Rank,
					TotalClubs = rankings.ClubRankings.Count,
					Points = row.Points,
					ClubName = row.ClubName,
					IsTopFive = row.Rank <= 5
				};
			}
		}

		// Next match without a result.
		var next = await _context.Matches
			.AsNoTracking()
			.Include(x => x.Tournament)
			.Include(x => x.HomeUser)
			.Include(x => x.AwayUser)
			.Include(x => x.HomeGameClub)
			.Include(x => x.AwayGameClub)
			.Where(x =>
				!x.IsDeleted &&
				x.Tournament.SeasonId == seasonId &&
				x.Status != MatchStatus.Finished &&
				x.Status != MatchStatus.Cancelled &&
				(x.HomeUserId == user.UserId || x.AwayUserId == user.UserId))
			.OrderBy(x => x.MatchDate)
			.ThenBy(x => x.WeekNumber)
			.FirstOrDefaultAsync();

		if (next is null)
			return;

		var isHome = next.HomeUserId == user.UserId;
		var opponentId = isHome ? next.AwayUserId : next.HomeUserId;
		var opponentUser = isHome ? next.AwayUser : next.HomeUser;
		var opponentGameClub = isHome ? next.AwayGameClub : next.HomeGameClub;

		var opponentClub = await _context.UserClubs
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.UserId == opponentId &&
				x.SeasonId == seasonId &&
				!x.IsDeleted);

		dto.NextMatch = new SidebarNextMatchDto
		{
			MatchId = next.MatchId,
			SeasonId = seasonId,
			WeekNumber = next.WeekNumber,
			IsHome = isHome,
			MatchDateUtc = next.MatchDate,
			Stage = next.Tournament?.Name ?? string.Empty,
			Opponent = opponentClub?.Name
				?? (!string.IsNullOrWhiteSpace(opponentUser?.ClubName)
					? opponentUser.ClubName
					: opponentGameClub?.Name ?? string.Empty),
			OpponentLogoUrl = !string.IsNullOrWhiteSpace(opponentClub?.LogoUrl)
				? opponentClub.LogoUrl
				: opponentGameClub?.LogoUrl ?? string.Empty
		};
	}
}
using backend.Data;
using backend.Entities.Clubs;
using backend.Entities.Enums;
using backend.Entities.Tournaments;
using backend.Features.Matches.Dtos;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.Matches.Services;

public class MatchService : IMatchService
{
	private readonly AppDbContext _context;

	public MatchService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<List<MatchWeekDto>> GetWeeksAsync(Guid seasonId)
	{
		var season = await _context.Seasons
			.AsNoTracking()
			.FirstOrDefaultAsync(x => !x.IsDeleted && x.SeasonId == seasonId);

		if (season is null)
			return [];

		var totalWeeks = season.TotalWeeks <= 0 ? 1 : season.TotalWeeks;

		return Enumerable.Range(1, totalWeeks)
			.Select(x => new MatchWeekDto
			{
				Value = x,
				Label = $"Week {x}"
			})
			.ToList();
	}

	public async Task<List<MatchDto>> GetByWeekAsync(
		Guid seasonId,
		int weekNumber,
		string? timeZone)
	{
		var userClubs = await GetUserClubMapAsync(seasonId);

		var matches = await _context.Matches
			.AsNoTracking()
			.Include(x => x.HomeUser)
			.Include(x => x.AwayUser)
			.Include(x => x.HomeGameClub)
			.Include(x => x.AwayGameClub)
			.Include(x => x.HomeUserClub)
			.Include(x => x.AwayUserClub)
			.Include(x => x.PlayerStats)
				.ThenInclude(x => x.User)
			.Include(x => x.Tournament)
			.Where(x =>
				!x.IsDeleted &&
				x.Tournament.SeasonId == seasonId &&
				x.WeekNumber == weekNumber)
			.OrderByDescending(x => x.IsMatchOfTheWeek)
			.ThenBy(x => x.MatchDate)
			.ToListAsync();

		return matches
			.Select(x => MapToDto(x, timeZone, userClubs))
			.ToList();
	}

	public async Task<MatchDto?> GetByIdAsync(
		Guid matchId,
		string? timeZone)
	{
		var match = await _context.Matches
			.AsNoTracking()
			.Include(x => x.HomeUser)
			.Include(x => x.AwayUser)
			.Include(x => x.HomeGameClub)
			.Include(x => x.AwayGameClub)
			.Include(x => x.HomeUserClub)
			.Include(x => x.AwayUserClub)
			.Include(x => x.PlayerStats)
				.ThenInclude(x => x.User)
			.Include(x => x.Tournament)
			.FirstOrDefaultAsync(x =>
				!x.IsDeleted &&
				x.MatchId == matchId);

		if (match is null)
			return null;

		var userClubs = await GetUserClubMapAsync(match.Tournament.SeasonId);

		return MapToDto(match, timeZone, userClubs);
	}

	public async Task<MatchResultDto?> UploadResultAsync(
		Guid matchId,
		UploadMatchResultDto dto)
	{
		var match = await _context.Matches
			.FirstOrDefaultAsync(x =>
				!x.IsDeleted &&
				x.MatchId == matchId);

		if (match is null)
			return null;

		match.HomeScore = dto.HomeScore;
		match.AwayScore = dto.AwayScore;
		match.Status = MatchStatus.Finished;

		await _context.SaveChangesAsync();

		return new MatchResultDto
		{
			MatchId = match.MatchId,
			HomeScore = match.HomeScore,
			AwayScore = match.AwayScore,
			Status = match.Status.ToString()
		};
	}

	public async Task<bool> GenerateScheduleAsync(Guid seasonId)
	{
		var season = await _context.Seasons
			.Include(x => x.Tournaments)
			.FirstOrDefaultAsync(x =>
				!x.IsDeleted &&
				x.SeasonId == seasonId);

		if (season is null)
			return false;

		var tournament = season.Tournaments
			.FirstOrDefault(x => !x.IsDeleted);

		if (tournament is null)
		{
			tournament = new Tournament
			{
				TournamentId = Guid.NewGuid(),
				SeasonId = season.SeasonId,
				Name = $"{season.Name} League",
				StartDate = season.StartDate,
				EndDate = season.EndDate
			};

			_context.Tournaments.Add(tournament);
			await _context.SaveChangesAsync();
		}

		var existingMatches = await _context.Matches
			.Include(x => x.Tournament)
			.Where(x =>
				!x.IsDeleted &&
				x.Tournament.SeasonId == seasonId)
			.ToListAsync();

		if (existingMatches.Count > 0)
		{
			_context.Matches.RemoveRange(existingMatches);
			await _context.SaveChangesAsync();
		}

		var teams = await _context.UserClubs
			.AsNoTracking()
			.Where(x =>
				x.SeasonId == seasonId &&
				!x.IsDeleted &&
				x.BaseGameClubId.HasValue)
			.Select(x => new ScheduleTeam
			{
				UserId = x.UserId,
				UserClubId = x.UserClubId,
				GameClubId = x.BaseGameClubId!.Value
			})
			.ToListAsync();

		if (teams.Count < 2)
			return false;

		var totalWeeks = season.TotalWeeks <= 0 ? 1 : season.TotalWeeks;
		var pairings = GenerateSchedulePairings(teams, totalWeeks);

		var matches = new List<Match>();
		var matchDate = season.StartDate.Date.AddHours(20);

		for (var i = 0; i < pairings.Count; i++)
		{
			var weekNumber = pairings[i].WeekNumber;
			var weekMatchCount = matches.Count(x => x.WeekNumber == weekNumber);

			matches.Add(new Match
			{
				MatchId = Guid.NewGuid(),
				TournamentId = tournament.TournamentId,

				HomeUserId = pairings[i].Home.UserId,
				AwayUserId = pairings[i].Away.UserId,

				HomeGameClubId = pairings[i].Home.GameClubId,
				AwayGameClubId = pairings[i].Away.GameClubId,

				HomeScore = 0,
				AwayScore = 0,

				MatchDate = matchDate
		.AddDays((weekNumber - 1) * 7)
		.AddHours(weekMatchCount),

				Status = MatchStatus.Scheduled,
				WeekNumber = weekNumber,
				IsMatchOfTheWeek = weekMatchCount == 0,

				Arena = string.Empty,
				Mode = "Best of 1"
			});
		}

		await _context.Matches.AddRangeAsync(matches);
		await _context.SaveChangesAsync();

		return true;
	}

	private async Task<Dictionary<Guid, UserClub>> GetUserClubMapAsync(Guid seasonId)
	{
		return await _context.UserClubs
			.AsNoTracking()
			.Include(x => x.BaseGameClub)
			.Where(x =>
				x.SeasonId == seasonId &&
				!x.IsDeleted)
			.ToDictionaryAsync(x => x.UserId);
	}

	private static MatchDto MapToDto(
		Match match,
		string? timeZone,
		Dictionary<Guid, UserClub> userClubs)
	{
		var localDate = ConvertToLocal(match.MatchDate, timeZone);

		var homeClub = match.HomeUserClub ??
			(match.HomeUserId != Guid.Empty &&
			 userClubs.TryGetValue(match.HomeUserId, out var hc)
				? hc
				: null);

		var awayClub = match.AwayUserClub ??
			(match.AwayUserId != Guid.Empty &&
			 userClubs.TryGetValue(match.AwayUserId, out var ac)
				? ac
				: null);

		var homeName = homeClub?.Name ??
			(!string.IsNullOrWhiteSpace(match.HomeUser?.ClubName)
				? match.HomeUser.ClubName
				: match.HomeGameClub.Name);

		var awayName = awayClub?.Name ??
			(!string.IsNullOrWhiteSpace(match.AwayUser?.ClubName)
				? match.AwayUser.ClubName
				: match.AwayGameClub.Name);

		var homeLogo = !string.IsNullOrWhiteSpace(homeClub?.LogoUrl)
	? homeClub.LogoUrl
	: "";

		var awayLogo = !string.IsNullOrWhiteSpace(awayClub?.LogoUrl)
			? awayClub.LogoUrl
			: "";

		return new MatchDto
		{
			MatchId = match.MatchId,
			WeekNumber = match.WeekNumber,
			IsMatchOfTheWeek = match.IsMatchOfTheWeek,
			Status = match.Status.ToString(),

			Arena = !string.IsNullOrWhiteSpace(match.Arena)
				? match.Arena
				: !string.IsNullOrWhiteSpace(match.HomeGameClub.StadiumName)
					? match.HomeGameClub.StadiumName
					: !string.IsNullOrWhiteSpace(match.AwayGameClub.StadiumName)
						? match.AwayGameClub.StadiumName
						: "Champions Arena",

			Mode = string.IsNullOrWhiteSpace(match.Mode)
				? "Best of 1"
				: match.Mode,

			MatchDate = localDate.ToString("MMM dd, yyyy"),
			MatchTime = localDate.ToString("hh:mm tt"),

			HomeScore = match.HomeScore,
			AwayScore = match.AwayScore,

			Home = new MatchTeamDto
			{
				ClubId = homeClub?.UserClubId ?? match.HomeGameClubId,
				UserId = match.HomeUserId,
				Name = homeName,
				Club = match.HomeGameClub.Name,
				LogoUrl = homeLogo,
				Stats = BuildTeamStats(match, match.HomeUserId)
			},

			Away = new MatchTeamDto
			{
				ClubId = awayClub?.UserClubId ?? match.AwayGameClubId,
				UserId = match.AwayUserId,
				Name = awayName,
				Club = match.AwayGameClub.Name,
				LogoUrl = awayLogo,
				Stats = BuildTeamStats(match, match.AwayUserId)
			}
		};
	}

	private static List<SchedulePairing> GenerateSchedulePairings(
		List<ScheduleTeam> teams,
		int totalWeeks)
	{
		var clubs = teams.ToList();

		if (clubs.Count % 2 != 0)
		{
			clubs.Add(new ScheduleTeam
			{
				UserId = Guid.Empty,
				UserClubId = Guid.Empty,
				GameClubId = Guid.Empty
			});
		}

		var totalClubs = clubs.Count;
		var matchesPerWeek = totalClubs / 2;
		var pairings = new List<SchedulePairing>();

		for (var week = 1; week <= totalWeeks; week++)
		{
			for (var match = 0; match < matchesPerWeek; match++)
			{
				var home = clubs[match];
				var away = clubs[totalClubs - 1 - match];

				if (home.UserId == Guid.Empty || away.UserId == Guid.Empty)
					continue;

				pairings.Add(new SchedulePairing
				{
					WeekNumber = week,
					Home = week % 2 == 1 ? home : away,
					Away = week % 2 == 1 ? away : home
				});
			}

			var fixedClub = clubs[0];
			var rotatingClubs = clubs.Skip(1).ToList();

			rotatingClubs.Insert(0, rotatingClubs.Last());
			rotatingClubs.RemoveAt(rotatingClubs.Count - 1);

			clubs = [fixedClub, .. rotatingClubs];
		}

		return pairings;
	}

	private static MatchTeamStatsDto BuildTeamStats(
		Match match,
		Guid userId)
	{
		var teamStats = match.PlayerStats
			.Where(x => x.UserId == userId)
			.ToList();

		var topScorer = teamStats
			.OrderByDescending(x => x.Goals)
			.FirstOrDefault();

		var topAssist = teamStats
			.OrderByDescending(x => x.Assists)
			.FirstOrDefault();

		return new MatchTeamStatsDto
		{
			TopScorer = topScorer?.User.DisplayName ?? "-",
			Goals = topScorer?.Goals ?? 0,
			TopAssist = topAssist?.User.DisplayName ?? "-",
			Assists = topAssist?.Assists ?? 0,
			Streak = ["W", "W", "L", "W"]
		};
	}

	private static DateTime ConvertToLocal(
		DateTime utcDate,
		string? timeZone)
	{
		if (string.IsNullOrWhiteSpace(timeZone))
			return utcDate;

		try
		{
			var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);

			return TimeZoneInfo.ConvertTimeFromUtc(
				DateTime.SpecifyKind(utcDate, DateTimeKind.Utc),
				zone);
		}
		catch
		{
			return utcDate;
		}
	}

	private sealed class ScheduleTeam
	{
		public Guid UserId { get; set; }

		public Guid UserClubId { get; set; }

		public Guid GameClubId { get; set; }
	}

	private sealed class SchedulePairing
	{
		public int WeekNumber { get; set; }

		public ScheduleTeam Home { get; set; } = null!;

		public ScheduleTeam Away { get; set; } = null!;
	}
}
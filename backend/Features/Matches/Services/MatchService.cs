using backend.Data;
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
			.FirstOrDefaultAsync(x =>
				!x.IsDeleted &&
				x.SeasonId == seasonId);

		if (season is null)
			return [];

		var totalWeeks = season.TotalWeeks <= 0
			? 1
			: season.TotalWeeks;

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
		string? timeZone
	)
	{
		var matches = await _context.Matches
			.Include(x => x.HomeGameClub)
				.ThenInclude(x => x.User)
			.Include(x => x.AwayGameClub)
				.ThenInclude(x => x.User)
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
			.Select(x => MapToDto(x, timeZone))
			.ToList();
	}

	public async Task<MatchDto?> GetByIdAsync(
		Guid matchId,
		string? timeZone
	)
	{
		var match = await _context.Matches
			.Include(x => x.HomeGameClub)
				.ThenInclude(x => x.User)
			.Include(x => x.AwayGameClub)
				.ThenInclude(x => x.User)
			.Include(x => x.PlayerStats)
				.ThenInclude(x => x.User)
			.Include(x => x.Tournament)
			.FirstOrDefaultAsync(x =>
				!x.IsDeleted &&
				x.MatchId == matchId);

		if (match is null)
			return null;

		return MapToDto(match, timeZone);
	}

	public async Task<MatchResultDto?> UploadResultAsync(
		Guid matchId,
		UploadMatchResultDto dto
	)
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
		match.UpdatedAtUTC = DateTime.UtcNow;

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
				EndDate = season.EndDate,
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

		var gameClubIds = await _context.SeasonRegistrations
			.AsNoTracking()
			.Where(x =>
				x.SeasonId == seasonId)
			.Select(x => x.GameClubId)
			.Distinct()
			.ToListAsync();

		if (gameClubIds.Count < 2)
			return false;

		var totalWeeks = season.TotalWeeks <= 0
			? 1
			: season.TotalWeeks;

		var pairings = GenerateSchedulePairings(gameClubIds, totalWeeks);

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

				HomeGameClubId = pairings[i].HomeGameClubId,
				AwayGameClubId = pairings[i].AwayGameClubId,

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

	private static List<(int WeekNumber, Guid HomeGameClubId, Guid AwayGameClubId)> GenerateSchedulePairings(
		List<Guid> gameClubIds,
		int totalWeeks
	)
	{
		var clubs = gameClubIds.ToList();

		if (clubs.Count % 2 != 0)
			clubs.Add(Guid.Empty);

		var totalClubs = clubs.Count;
		var matchesPerWeek = totalClubs / 2;

		var pairings = new List<(int WeekNumber, Guid HomeGameClubId, Guid AwayGameClubId)>();

		for (var week = 1; week <= totalWeeks; week++)
		{
			for (var match = 0; match < matchesPerWeek; match++)
			{
				var home = clubs[match];
				var away = clubs[totalClubs - 1 - match];

				if (home == Guid.Empty || away == Guid.Empty)
					continue;

				if (week % 2 == 1)
					pairings.Add((week, home, away));
				else
					pairings.Add((week, away, home));
			}

			var fixedClub = clubs[0];
			var rotatingClubs = clubs.Skip(1).ToList();

			rotatingClubs.Insert(0, rotatingClubs.Last());
			rotatingClubs.RemoveAt(rotatingClubs.Count - 1);

			clubs = [fixedClub, .. rotatingClubs];
		}

		return pairings;
	}

	private static MatchDto MapToDto(
		Match match,
		string? timeZone
	)
	{
		var localDate = ConvertToLocal(match.MatchDate, timeZone);

		var homeName = match.HomeGameClub.User is null ||
			string.IsNullOrWhiteSpace(match.HomeGameClub.User.ClubName)
				? match.HomeGameClub.Name
				: match.HomeGameClub.User.ClubName;

		var awayName = match.AwayGameClub.User is null ||
			string.IsNullOrWhiteSpace(match.AwayGameClub.User.ClubName)
				? match.AwayGameClub.Name
				: match.AwayGameClub.User.ClubName;

		return new MatchDto
		{
			MatchId = match.MatchId,
			WeekNumber = match.WeekNumber,
			IsMatchOfTheWeek = match.IsMatchOfTheWeek,
			Status = match.Status.ToString(),

			Arena = !string.IsNullOrWhiteSpace(match.HomeGameClub.StadiumName)
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
				ClubId = match.HomeGameClubId,
				Name = homeName,
				Club = match.HomeGameClub.Name,
				LogoUrl = match.HomeGameClub.LogoUrl ?? "",
				Stats = BuildTeamStats(match, match.HomeGameClubId)
			},

			Away = new MatchTeamDto
			{
				ClubId = match.AwayGameClubId,
				Name = awayName,
				Club = match.AwayGameClub.Name,
				LogoUrl = match.AwayGameClub.LogoUrl ?? "",
				Stats = BuildTeamStats(match, match.AwayGameClubId)
			}
		};
	}

	private static MatchTeamStatsDto BuildTeamStats(
		Match match,
		Guid gameClubId
	)
	{
		var teamStats = match.PlayerStats
			.Where(x => x.TeamId == gameClubId)
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
		string? timeZone
	)
	{
		if (string.IsNullOrWhiteSpace(timeZone))
			return utcDate;

		try
		{
			var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);

			return TimeZoneInfo.ConvertTimeFromUtc(
				DateTime.SpecifyKind(utcDate, DateTimeKind.Utc),
				zone
			);
		}
		catch
		{
			return utcDate;
		}
	}
}
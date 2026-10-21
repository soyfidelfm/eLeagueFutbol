using backend.Data;
using backend.Entities.Clubs;
using backend.Entities.Enums;
using backend.Entities.MatchStats;
using backend.Entities.MatchSummary;
using backend.Entities.Stats;
using backend.Entities.Tournaments;
using backend.Features.Matches.Dtos;
using backend.Features.Messages.Services;
using backend.Features.Notifications.Services;
using backend.Features.Realtime;
using backend.Features.Stats.Dtos;
using backend.Features.Stats.Services;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;

namespace backend.Features.Matches.Services;

public class MatchService : IMatchService
{
	private readonly AppDbContext _context;
	private readonly IMatchStatsOcrService _ocrService;
	private readonly IMessageService _messageService;
	private readonly INotificationService _notificationService;
	private readonly IAppRealtimeService _realtime;

	public MatchService(
		AppDbContext context,
		IMatchStatsOcrService ocrService,
		IMessageService messageService,
		INotificationService notificationService,
		IAppRealtimeService realtime)
	{
		_context = context;
		_ocrService = ocrService;
		_messageService = messageService;
		_notificationService = notificationService;
		_realtime = realtime;
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

		var userIds = matches
			.SelectMany(x => new[] { x.HomeUserId, x.AwayUserId })
			.Where(x => x != Guid.Empty)
			.Distinct()
			.ToList();

		var streaks = await GetTeamStreaksAsync(seasonId, userIds);

		return matches
			.Select(x => MapToDto(x, timeZone, userClubs, streaks))
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

		var userIds = new List<Guid>
		{
			match.HomeUserId,
			match.AwayUserId
		};

		var streaks = await GetTeamStreaksAsync(match.Tournament.SeasonId, userIds);

		return MapToDto(match, timeZone, userClubs, streaks);
	}

	public async Task<MatchResultDto?> UploadResultAsync(
	Guid matchId,
	UploadMatchResultDto dto)
	{
		var match = await _context.Matches
			.Include(x => x.Tournament)
			.Include(x => x.HomeUserClub)
			.Include(x => x.AwayUserClub)
			.FirstOrDefaultAsync(x =>
				!x.IsDeleted &&
				x.MatchId == matchId);

		if (match is null)
			return null;

		match.HomeScore = dto.HomeScore;
		match.AwayScore = dto.AwayScore;
		match.Status = MatchStatus.Finished;

		await _context.SaveChangesAsync();

		var homeClubName = match.HomeUserClub?.Name ?? "Home";
		var awayClubName = match.AwayUserClub?.Name ?? "Away";

		var resultText = $"{homeClubName} {match.HomeScore} - {match.AwayScore} {awayClubName}";

		var userIds = new List<Guid>();

		if (match.HomeUserId != Guid.Empty)
			userIds.Add(match.HomeUserId);

		if (match.AwayUserId != Guid.Empty)
			userIds.Add(match.AwayUserId);

		userIds = userIds
			.Distinct()
			.ToList();

		foreach (var userId in userIds)
		{
			await _messageService.SendSystemMessageAsync(
	userId,
	"Match Result Submitted",
	$"The match has been completed. Final score: {resultText}",
	"match_result",
	"/matches");

			await _notificationService.SendToUserAsync(
				userId,
				"Match Result Submitted",
				$"Final score: {resultText}");
		}

		var seasonId = match.Tournament?.SeasonId ?? Guid.Empty;

		if (seasonId != Guid.Empty)
			await _realtime.PublishDraftEventAsync(seasonId, "matchResult");

		return new MatchResultDto
		{
			MatchId = match.MatchId,
			HomeScore = match.HomeScore,
			AwayScore = match.AwayScore,
			Status = match.Status.ToString()
		};
	}

	public async Task<MatchStatsUploadResultDto?> UploadStatsImageAsync(
	Guid matchId,
	UploadMatchStatsImageDto dto)
	{
		var match = await _context.Matches
			.FirstOrDefaultAsync(x =>
				!x.IsDeleted &&
				x.MatchId == matchId);

		if (match is null)
			return null;

		object rawPreview = dto.Type switch
		{
			MatchStatsUploadType.MatchSummary =>
				await _ocrService.PreviewMatchSummaryAsync(dto.Image),

			MatchStatsUploadType.HomeTeamPerformance or
			MatchStatsUploadType.AwayTeamPerformance =>
				await _ocrService.PreviewTeamPerformanceAsync(dto.Image),

			_ => throw new ArgumentOutOfRangeException(nameof(dto.Type))
		};

		var rawJson = JsonSerializer.Serialize(rawPreview);

		var existing = await _context.MatchStatsUploads
			.FirstOrDefaultAsync(x =>
				x.MatchId == matchId &&
				x.Type == dto.Type);

		if (existing is null)
		{
			existing = new MatchStatsUpload
			{
				MatchStatsUploadId = Guid.NewGuid(),
				MatchId = matchId,
				UploadedByUserId = Guid.Empty,
				Type = dto.Type,
				ImageUrl = string.Empty,
				RawJson = rawJson,
				IsProcessed = false,
				CreatedAtUTC = DateTime.UtcNow
			};

			_context.MatchStatsUploads.Add(existing);
		}
		else
		{
			existing.RawJson = rawJson;
			existing.ImageUrl = string.Empty;
			existing.IsProcessed = false;
			existing.CreatedAtUTC = DateTime.UtcNow;
		}

		await _context.SaveChangesAsync();

		var uploads = await _context.MatchStatsUploads
			.Where(x => x.MatchId == matchId)
			.ToListAsync();

		var hasSummary = uploads.Any(x =>
			x.Type == MatchStatsUploadType.MatchSummary);

		var hasHome = uploads.Any(x =>
			x.Type == MatchStatsUploadType.HomeTeamPerformance);

		var hasAway = uploads.Any(x =>
			x.Type == MatchStatsUploadType.AwayTeamPerformance);

		var isReady = hasSummary && hasHome && hasAway;

		if (isReady)
		{
			await ProcessUploadedStatsAsync(matchId);

			uploads = await _context.MatchStatsUploads
				.AsNoTracking()
				.Where(x => x.MatchId == matchId)
				.ToListAsync();
		}

		return new MatchStatsUploadResultDto
		{
			MatchId = matchId,
			HasMatchSummary = hasSummary,
			HasHomeTeamPerformance = hasHome,
			HasAwayTeamPerformance = hasAway,
			IsReadyToProcess = isReady,
			Status = isReady ? "Ready" : "Pending"
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

		var teams = await _context.SeasonRegistrations
	.AsNoTracking()
	.Where(x => x.SeasonId == seasonId)
	.Join(
		_context.UserClubs.AsNoTracking().Where(x =>
			x.SeasonId == seasonId &&
			!x.IsDeleted &&
			x.BaseGameClubId.HasValue),
		sr => new { sr.SeasonId, sr.UserId },
		uc => new { uc.SeasonId, uc.UserId },
		(sr, uc) => new ScheduleTeam
		{
			UserId = sr.UserId,
			UserClubId = uc.UserClubId,
			GameClubId = sr.GameClubId
		})
	.ToListAsync();

		if (teams.Count < 2)
			return false;

		teams = teams
			.OrderBy(_ => Guid.NewGuid())
			.ToList();

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

	private async Task<Dictionary<Guid, List<string>>> GetTeamStreaksAsync(
		Guid seasonId,
		List<Guid> userIds)
	{
		userIds = userIds
			.Where(x => x != Guid.Empty)
			.Distinct()
			.ToList();

		if (userIds.Count == 0)
			return [];

		var finishedMatches = await _context.Matches
			.AsNoTracking()
			.Include(x => x.Tournament)
			.Where(x =>
				!x.IsDeleted &&
				x.Tournament.SeasonId == seasonId &&
				x.Status == MatchStatus.Finished &&
				(userIds.Contains(x.HomeUserId) ||
				 userIds.Contains(x.AwayUserId)))
			.OrderByDescending(x => x.MatchDate)
			.ToListAsync();

		var result = new Dictionary<Guid, List<string>>();

		foreach (var userId in userIds)
		{
			var streak = finishedMatches
				.Where(x => x.HomeUserId == userId || x.AwayUserId == userId)
				.Take(5)
				.Select(x =>
				{
					var isHome = x.HomeUserId == userId;

					var goalsFor = isHome
						? x.HomeScore
						: x.AwayScore;

					var goalsAgainst = isHome
						? x.AwayScore
						: x.HomeScore;

					if (goalsFor > goalsAgainst)
						return "W";

					if (goalsFor < goalsAgainst)
						return "L";

					return "D";
				})
				.ToList();

			result[userId] = streak;
		}

		return result;
	}

	private static MatchDto MapToDto(
		Match match,
		string? timeZone,
		Dictionary<Guid, UserClub> userClubs,
		Dictionary<Guid, List<string>> streaks)
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
				Stats = BuildTeamStats(match, match.HomeUserId, streaks)
			},

			Away = new MatchTeamDto
			{
				ClubId = awayClub?.UserClubId ?? match.AwayGameClubId,
				UserId = match.AwayUserId,
				Name = awayName,
				Club = match.AwayGameClub.Name,
				LogoUrl = awayLogo,
				Stats = BuildTeamStats(match, match.AwayUserId, streaks)
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
	Guid userId,
	Dictionary<Guid, List<string>> streaks)
	{
		var teamStats = match.PlayerStats
			.Where(x => x.UserId == userId)
			.ToList();

		var topScorer = teamStats
			.OrderByDescending(x => x.Goals)
			.ThenByDescending(x => x.Rating)
			.FirstOrDefault();

		var topAssist = teamStats
			.OrderByDescending(x => x.Assists)
			.ThenByDescending(x => x.Rating)
			.FirstOrDefault();

		return new MatchTeamStatsDto
		{
			TopScorer = topScorer?.PlayerName ?? "-",
			Goals = topScorer?.Goals ?? 0,

			TopAssist = topAssist?.PlayerName ?? "-",
			Assists = topAssist?.Assists ?? 0,

			Streak = streaks.TryGetValue(userId, out var streak)
				? streak
				: []
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

	public async Task ApplyOcrStatsAsync(
		Guid matchId,
		string summaryJson,
		string homePerformanceJson,
		string awayPerformanceJson)
	{
		var payloads = new Dictionary<MatchStatsUploadType, string>
		{
			[MatchStatsUploadType.MatchSummary] = summaryJson,
			[MatchStatsUploadType.HomeTeamPerformance] = homePerformanceJson,
			[MatchStatsUploadType.AwayTeamPerformance] = awayPerformanceJson
		};

		var existing = await _context.MatchStatsUploads
			.Where(x => x.MatchId == matchId)
			.ToListAsync();

		foreach (var (type, json) in payloads)
		{
			var upload = existing.FirstOrDefault(x => x.Type == type);

			if (upload is null)
			{
				_context.MatchStatsUploads.Add(new MatchStatsUpload
				{
					MatchStatsUploadId = Guid.NewGuid(),
					MatchId = matchId,
					UploadedByUserId = Guid.Empty,
					Type = type,
					ImageUrl = string.Empty,
					RawJson = json,
					IsProcessed = false,
					CreatedAtUTC = DateTime.UtcNow
				});
			}
			else
			{
				upload.RawJson = json;
				upload.IsProcessed = false;
				upload.UpdatedAtUTC = DateTime.UtcNow;
			}
		}

		await _context.SaveChangesAsync();
		await ProcessUploadedStatsAsync(matchId);
	}

	private async Task ProcessUploadedStatsAsync(Guid matchId)
	{
		var match = await _context.Matches
			.Include(x => x.Tournament)
			.Include(x => x.HomeUserClub)
			.Include(x => x.AwayUserClub)
			.FirstOrDefaultAsync(x =>
				!x.IsDeleted &&
				x.MatchId == matchId);

		if (match is null)
			return;

		var homeUserClubId = match.HomeUserClub?.UserClubId;

		if (!homeUserClubId.HasValue)
		{
			homeUserClubId = await _context.UserClubs
				.Where(x =>
					!x.IsDeleted &&
					x.SeasonId == match.Tournament.SeasonId &&
					x.UserId == match.HomeUserId)
				.Select(x => (Guid?)x.UserClubId)
				.FirstOrDefaultAsync();
		}

		var awayUserClubId = match.AwayUserClub?.UserClubId;

		if (!awayUserClubId.HasValue)
		{
			awayUserClubId = await _context.UserClubs
				.Where(x =>
					!x.IsDeleted &&
					x.SeasonId == match.Tournament.SeasonId &&
					x.UserId == match.AwayUserId)
				.Select(x => (Guid?)x.UserClubId)
				.FirstOrDefaultAsync();
		}

		if (!homeUserClubId.HasValue || !awayUserClubId.HasValue)
			throw new InvalidOperationException("Home or away UserClub was not found.");

		var uploads = await _context.MatchStatsUploads
			.Where(x => x.MatchId == matchId)
			.ToListAsync();

		var summaryUpload = uploads.FirstOrDefault(x =>
			x.Type == MatchStatsUploadType.MatchSummary);

		var homeUpload = uploads.FirstOrDefault(x =>
			x.Type == MatchStatsUploadType.HomeTeamPerformance);

		var awayUpload = uploads.FirstOrDefault(x =>
			x.Type == MatchStatsUploadType.AwayTeamPerformance);

		if (string.IsNullOrWhiteSpace(summaryUpload?.RawJson) ||
			string.IsNullOrWhiteSpace(homeUpload?.RawJson) ||
			string.IsNullOrWhiteSpace(awayUpload?.RawJson))
			return;

		var summary = JsonSerializer.Deserialize<Stats.Dtos.MatchSummaryStatsOcrDto>(
			summaryUpload.RawJson);

		var homePerformance = JsonSerializer.Deserialize<TeamPerformanceOcrPreviewDto>(
			homeUpload.RawJson);

		var awayPerformance = JsonSerializer.Deserialize<TeamPerformanceOcrPreviewDto>(
			awayUpload.RawJson);

		if (summary is null || homePerformance is null || awayPerformance is null)
			return;

		var oldSummary = await _context.MatchSummaryStats
			.Where(x => x.MatchId == matchId)
			.ToListAsync();

		var oldPlayerStats = await _context.MatchPlayerStats
			.Where(x => x.MatchId == matchId)
			.ToListAsync();

		_context.MatchSummaryStats.RemoveRange(oldSummary);
		_context.MatchPlayerStats.RemoveRange(oldPlayerStats);

		_context.MatchSummaryStats.Add(new MatchSummaryStats
		{
			MatchSummaryStatsId = Guid.NewGuid(),
			MatchId = matchId,

			HomePossession = summary.HomePossession,
			AwayPossession = summary.AwayPossession,
			HomeShots = summary.HomeShots,
			AwayShots = summary.AwayShots,
			HomeExpectedGoals = summary.HomeExpectedGoals,
			AwayExpectedGoals = summary.AwayExpectedGoals,
			HomePasses = summary.HomePasses,
			AwayPasses = summary.AwayPasses,
			HomeTackles = summary.HomeTackles,
			AwayTackles = summary.AwayTackles,
			HomeTacklesWon = summary.HomeTacklesWon,
			AwayTacklesWon = summary.AwayTacklesWon,
			HomeInterceptions = summary.HomeInterceptions,
			AwayInterceptions = summary.AwayInterceptions,
			HomeSaves = summary.HomeSaves,
			AwaySaves = summary.AwaySaves,
			HomeFoulsCommitted = summary.HomeFoulsCommitted,
			AwayFoulsCommitted = summary.AwayFoulsCommitted,
			HomeOffsides = summary.HomeOffsides,
			AwayOffsides = summary.AwayOffsides,
			HomeCorners = summary.HomeCorners,
			AwayCorners = summary.AwayCorners,
			HomeFreeKicks = summary.HomeFreeKicks,
			AwayFreeKicks = summary.AwayFreeKicks,
			HomePenaltyKicks = summary.HomePenaltyKicks,
			AwayPenaltyKicks = summary.AwayPenaltyKicks,
			HomeYellowCards = summary.HomeYellowCards,
			AwayYellowCards = summary.AwayYellowCards
		});

		foreach (var player in homePerformance.Players)
		{
			_context.MatchPlayerStats.Add(new MatchPlayerStats
			{
				MatchPlayerStatsId = Guid.NewGuid(),
				MatchId = matchId,
				UserId = match.HomeUserId,
				UserClubId = homeUserClubId.Value,

				Position = player.Position,
				PlayerName = player.PlayerName,
				Rating = player.Rating,
				Goals = player.Goals,
				Assists = player.Assists
			});
		}

		foreach (var player in awayPerformance.Players)
		{
			_context.MatchPlayerStats.Add(new MatchPlayerStats
			{
				MatchPlayerStatsId = Guid.NewGuid(),
				MatchId = matchId,
				UserId = match.AwayUserId,
				UserClubId = awayUserClubId.Value,

				Position = player.Position,
				PlayerName = player.PlayerName,
				Rating = player.Rating,
				Goals = player.Goals,
				Assists = player.Assists
			});
		}

		foreach (var upload in uploads)
		{
			upload.IsProcessed = true;
		}

		match.HomeScore = summary.HomeGoals;
		match.AwayScore = summary.AwayGoals;
		match.Status = MatchStatus.Finished;

		Debug.WriteLine("===== SUMMARY OCR RAW LINES =====");

		foreach (var line in summary.RawLines)
		{
			Debug.WriteLine($"OCR: [{line}]");
		}

		Debug.WriteLine($"Detected Score: {summary.HomeGoals}-{summary.AwayGoals}");
		await _context.SaveChangesAsync();
	}

	public async Task<SaveManualMatchStatsDto?> GetManualStatsAsync(
	Guid matchId)
	{
		var match = await _context.Matches
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				!x.IsDeleted &&
				x.MatchId == matchId);

		if (match is null)
			return null;

		var summary = await _context.MatchSummaryStats
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.MatchId == matchId);

		var savedPlayers = await _context.MatchPlayerStats
			.AsNoTracking()
			.Where(x => x.MatchId == matchId)
			.OrderBy(x => x.Position)
			.ThenByDescending(x => x.Rating)
			.ToListAsync();

		var result = new SaveManualMatchStatsDto
		{
			HomeGoals = Convert.ToInt32(match.HomeScore),
			AwayGoals = Convert.ToInt32(match.AwayScore),

			HomePossession = summary?.HomePossession ?? 0,
			AwayPossession = summary?.AwayPossession ?? 0,

			HomeShots = summary?.HomeShots ?? 0,
			AwayShots = summary?.AwayShots ?? 0,

			HomeExpectedGoals = summary?.HomeExpectedGoals ?? 0,
			AwayExpectedGoals = summary?.AwayExpectedGoals ?? 0,

			HomePasses = summary?.HomePasses ?? 0,
			AwayPasses = summary?.AwayPasses ?? 0,

			HomeTackles = summary?.HomeTackles ?? 0,
			AwayTackles = summary?.AwayTackles ?? 0,

			HomeTacklesWon = summary?.HomeTacklesWon ?? 0,
			AwayTacklesWon = summary?.AwayTacklesWon ?? 0,

			HomeInterceptions = summary?.HomeInterceptions ?? 0,
			AwayInterceptions = summary?.AwayInterceptions ?? 0,

			HomeSaves = summary?.HomeSaves ?? 0,
			AwaySaves = summary?.AwaySaves ?? 0,

			HomeFoulsCommitted = summary?.HomeFoulsCommitted ?? 0,
			AwayFoulsCommitted = summary?.AwayFoulsCommitted ?? 0,

			HomeOffsides = summary?.HomeOffsides ?? 0,
			AwayOffsides = summary?.AwayOffsides ?? 0,

			HomeCorners = summary?.HomeCorners ?? 0,
			AwayCorners = summary?.AwayCorners ?? 0,

			HomeFreeKicks = summary?.HomeFreeKicks ?? 0,
			AwayFreeKicks = summary?.AwayFreeKicks ?? 0,

			HomePenaltyKicks = summary?.HomePenaltyKicks ?? 0,
			AwayPenaltyKicks = summary?.AwayPenaltyKicks ?? 0,

			HomeYellowCards = summary?.HomeYellowCards ?? 0,
			AwayYellowCards = summary?.AwayYellowCards ?? 0,

			HomePlayers = [],
			AwayPlayers = []
		};

		foreach (var player in savedPlayers
			.Where(x => x.UserId == match.HomeUserId))
		{
			result.HomePlayers.Add(new()
			{
				Position = player.Position,
				PlayerName = player.PlayerName,
				Rating = player.Rating,
				Goals = player.Goals,
				Assists = player.Assists
			});
		}

		foreach (var player in savedPlayers
			.Where(x => x.UserId == match.AwayUserId))
		{
			result.AwayPlayers.Add(new()
			{
				Position = player.Position,
				PlayerName = player.PlayerName,
				Rating = player.Rating,
				Goals = player.Goals,
				Assists = player.Assists
			});
		}

		return result;
	}
	public async Task<bool> SaveManualStatsAsync(
	Guid matchId,
	SaveManualMatchStatsDto dto)
	{
		var match = await _context.Matches
			.Include(x => x.Tournament)
			.Include(x => x.HomeUserClub)
			.Include(x => x.AwayUserClub)
			.FirstOrDefaultAsync(x =>
				!x.IsDeleted &&
				x.MatchId == matchId);

		if (match is null)
			return false;

		var homeUserClubId = match.HomeUserClub?.UserClubId;

		if (!homeUserClubId.HasValue)
		{
			homeUserClubId = await _context.UserClubs
				.Where(x =>
					!x.IsDeleted &&
					x.SeasonId == match.Tournament.SeasonId &&
					x.UserId == match.HomeUserId)
				.Select(x => (Guid?)x.UserClubId)
				.FirstOrDefaultAsync();
		}

		var awayUserClubId = match.AwayUserClub?.UserClubId;

		if (!awayUserClubId.HasValue)
		{
			awayUserClubId = await _context.UserClubs
				.Where(x =>
					!x.IsDeleted &&
					x.SeasonId == match.Tournament.SeasonId &&
					x.UserId == match.AwayUserId)
				.Select(x => (Guid?)x.UserClubId)
				.FirstOrDefaultAsync();
		}

		if (!homeUserClubId.HasValue || !awayUserClubId.HasValue)
		{
			throw new InvalidOperationException(
				"Home or away UserClub was not found.");
		}

		await using var transaction =
			await _context.Database.BeginTransactionAsync();

		try
		{
			var oldSummary = await _context.MatchSummaryStats
				.Where(x => x.MatchId == matchId)
				.ToListAsync();

			var oldPlayerStats = await _context.MatchPlayerStats
				.Where(x => x.MatchId == matchId)
				.ToListAsync();

			_context.MatchSummaryStats.RemoveRange(oldSummary);
			_context.MatchPlayerStats.RemoveRange(oldPlayerStats);

			var summary = new MatchSummaryStats
			{
				MatchSummaryStatsId = Guid.NewGuid(),
				MatchId = matchId,

				HomePossession = dto.HomePossession,
				AwayPossession = dto.AwayPossession,

				HomeShots = dto.HomeShots,
				AwayShots = dto.AwayShots,

				HomeExpectedGoals = dto.HomeExpectedGoals,
				AwayExpectedGoals = dto.AwayExpectedGoals,

				HomePasses = dto.HomePasses,
				AwayPasses = dto.AwayPasses,

				HomeTackles = dto.HomeTackles,
				AwayTackles = dto.AwayTackles,

				HomeTacklesWon = dto.HomeTacklesWon,
				AwayTacklesWon = dto.AwayTacklesWon,

				HomeInterceptions = dto.HomeInterceptions,
				AwayInterceptions = dto.AwayInterceptions,

				HomeSaves = dto.HomeSaves,
				AwaySaves = dto.AwaySaves,

				HomeFoulsCommitted = dto.HomeFoulsCommitted,
				AwayFoulsCommitted = dto.AwayFoulsCommitted,

				HomeOffsides = dto.HomeOffsides,
				AwayOffsides = dto.AwayOffsides,

				HomeCorners = dto.HomeCorners,
				AwayCorners = dto.AwayCorners,

				HomeFreeKicks = dto.HomeFreeKicks,
				AwayFreeKicks = dto.AwayFreeKicks,

				HomePenaltyKicks = dto.HomePenaltyKicks,
				AwayPenaltyKicks = dto.AwayPenaltyKicks,

				HomeYellowCards = dto.HomeYellowCards,
				AwayYellowCards = dto.AwayYellowCards
			};

			_context.MatchSummaryStats.Add(summary);

			foreach (var player in dto.HomePlayers)
			{
				_context.MatchPlayerStats.Add(new MatchPlayerStats
				{
					MatchPlayerStatsId = Guid.NewGuid(),
					MatchId = matchId,

					UserId = match.HomeUserId,
					UserClubId = homeUserClubId.Value,

					Position = player.Position.Trim(),
					PlayerName = player.PlayerName.Trim(),
					Rating = player.Rating,
					Goals = player.Goals,
					Assists = player.Assists
				});
			}

			foreach (var player in dto.AwayPlayers)
			{
				_context.MatchPlayerStats.Add(new MatchPlayerStats
				{
					MatchPlayerStatsId = Guid.NewGuid(),
					MatchId = matchId,

					UserId = match.AwayUserId,
					UserClubId = awayUserClubId.Value,

					Position = player.Position.Trim(),
					PlayerName = player.PlayerName.Trim(),
					Rating = player.Rating,
					Goals = player.Goals,
					Assists = player.Assists
				});
			}

			match.HomeScore = dto.HomeGoals;
			match.AwayScore = dto.AwayGoals;
			match.Status = MatchStatus.Finished;

			await _context.SaveChangesAsync();
			await transaction.CommitAsync();

			return true;
		}
		catch
		{
			await transaction.RollbackAsync();
			throw;
		}
	}

	public async Task<MatchPlayerOptionsDto?>
	GetMatchPlayerOptionsAsync(Guid matchId)
	{
		var result = await _context.Matches
			.AsNoTracking()
			.Where(match =>
				!match.IsDeleted &&
				match.MatchId == matchId)
			.Select(match => new MatchPlayerOptionsDto
			{
				HomeTeamPlayers = match.HomeGameClub.Players
					.OrderBy(player => player.Position)
					.ThenByDescending(player => player.Overall)
					.ThenBy(player => player.Name)
					.Select(player => new TeamPlayerOptionDto
					{
						GamePlayerId =
							player.GamePlayerId,

						Name =
							player.FullName != null &&
							player.FullName != string.Empty
								? player.FullName
								: player.Name,

						Position =
	player.Position == null
		? string.Empty
		: player.Position.Contains(",")
			? player.Position
				.Substring(0, player.Position.IndexOf(","))
				.Trim()
			: player.Position.Trim(),

						Overall =
							player.Overall,

						ImageUrl =
							player.ImageUrl
					})
					.ToList(),

				AwayTeamPlayers = match.AwayGameClub.Players
					.OrderBy(player => player.Position)
					.ThenByDescending(player => player.Overall)
					.ThenBy(player => player.Name)
					.Select(player => new TeamPlayerOptionDto
					{
						GamePlayerId =
							player.GamePlayerId,

						Name =
							player.FullName != null &&
							player.FullName != string.Empty
								? player.FullName
								: player.Name,

						Position =
	player.Position == null
		? string.Empty
		: player.Position.Contains(",")
			? player.Position
				.Substring(0, player.Position.IndexOf(","))
				.Trim()
			: player.Position.Trim(),

						Overall =
							player.Overall,

						ImageUrl =
							player.ImageUrl
					})
					.ToList()
			})
			.FirstOrDefaultAsync();

		return result;
	}
}
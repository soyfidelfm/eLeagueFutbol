using System.Text.Json;
using backend.Data;
using backend.Entities.Enums;
using backend.Entities.Identity;
using backend.Entities.MatchStats;
using backend.Entities.Tournaments;
using backend.Features.Matches.Dtos;
using backend.Features.Stats.Dtos;
using backend.Features.Stats.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.Matches.Services;

/// <summary>
/// OCR result proposals: the uploader submits three screenshots, the result stays
/// Pending until the opponent or an admin accepts (stats saved, match Finished,
/// matchResult published once through MatchService.UploadResultAsync) or rejects it.
/// </summary>
public class PendingMatchResultService : IPendingMatchResultService
{
	private readonly AppDbContext _context;
	private readonly IMatchStatsOcrService _ocrService;
	private readonly IMatchService _matchService;

	public PendingMatchResultService(
		AppDbContext context,
		IMatchStatsOcrService ocrService,
		IMatchService matchService)
	{
		_context = context;
		_ocrService = ocrService;
		_matchService = matchService;
	}

	public async Task<PendingMatchResultDto> SubmitAsync(
		Guid matchId,
		string auth0Id,
		SubmitMatchResultProposalDto dto)
	{
		var user = await GetUserAsync(auth0Id);
		var match = await GetOpenMatchAsync(matchId);

		if (!IsAdmin(user) && !IsParticipant(user, match))
			throw new UnauthorizedAccessException("Only the match participants or an admin can submit a result.");

		if (dto.SummaryImage is null || dto.HomePerformanceImage is null || dto.AwayPerformanceImage is null)
			throw new ArgumentException("All three screenshots are required: Match Summary, Home Performance and Away Performance.");

		if (await HasPendingAsync(matchId))
			throw new InvalidOperationException("A pending result already exists for this match.");

		// OCR only: nothing is written to the match here.
		var summary = await _ocrService.PreviewMatchSummaryAsync(dto.SummaryImage);
		var home = await _ocrService.PreviewTeamPerformanceAsync(dto.HomePerformanceImage);
		var away = await _ocrService.PreviewTeamPerformanceAsync(dto.AwayPerformanceImage);

		// Re-check after the (slow) OCR calls to narrow the race window.
		if (await HasPendingAsync(matchId))
			throw new InvalidOperationException("A pending result already exists for this match.");

		var proposal = new PendingMatchResult
		{
			PendingMatchResultId = Guid.NewGuid(),
			MatchId = matchId,
			SubmittedByUserId = user.UserId,
			Status = PendingMatchResultStatus.Pending,
			HomeScore = summary.HomeGoals,
			AwayScore = summary.AwayGoals,
			SummaryJson = JsonSerializer.Serialize(summary),
			HomePerformanceJson = JsonSerializer.Serialize(home),
			AwayPerformanceJson = JsonSerializer.Serialize(away),
			CreatedAtUTC = DateTime.UtcNow,
			CreatedByUserId = user.UserId
		};

		_context.PendingMatchResults.Add(proposal);

		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateException)
		{
			// Unique filtered index: another pending proposal was saved concurrently.
			throw new InvalidOperationException("A pending result already exists for this match.");
		}

		return ToDto(proposal, user, match, user.DisplayName);
	}

	public async Task<PendingMatchResultDto?> GetPendingAsync(Guid matchId, string auth0Id)
	{
		var user = await GetUserAsync(auth0Id);

		var match = await _context.Matches
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.MatchId == matchId && !x.IsDeleted)
			?? throw new KeyNotFoundException("Match not found.");

		var proposal = await _context.PendingMatchResults
			.AsNoTracking()
			.Where(x =>
				x.MatchId == matchId &&
				!x.IsDeleted &&
				x.Status == PendingMatchResultStatus.Pending)
			.OrderByDescending(x => x.CreatedAtUTC)
			.FirstOrDefaultAsync();

		if (proposal is null)
			return null;

		var submitterName = await _context.Users
			.AsNoTracking()
			.Where(x => x.UserId == proposal.SubmittedByUserId)
			.Select(x => x.DisplayName)
			.FirstOrDefaultAsync() ?? "Unknown";

		return ToDto(proposal, user, match, submitterName);
	}

	public async Task<MatchResultDto> AcceptAsync(Guid matchId, Guid proposalId, string auth0Id)
	{
		var user = await GetUserAsync(auth0Id);
		var match = await GetOpenMatchAsync(matchId);
		var proposal = await GetPendingProposalAsync(matchId, proposalId);

		EnsureCanReview(user, match, proposal);

		// Persist stats from the stored OCR data (also sets scores and Finished).
		await _matchService.ApplyOcrStatsAsync(
			matchId,
			proposal.SummaryJson,
			proposal.HomePerformanceJson,
			proposal.AwayPerformanceJson);

		proposal.Status = PendingMatchResultStatus.Accepted;
		proposal.ReviewedByUserId = user.UserId;
		proposal.ReviewedAtUTC = DateTime.UtcNow;
		proposal.UpdatedAtUTC = DateTime.UtcNow;
		proposal.UpdatedByUserId = user.UserId;

		await _context.SaveChangesAsync();

		// Notifies both players and publishes matchResult exactly once.
		var result = await _matchService.UploadResultAsync(matchId, new UploadMatchResultDto
		{
			HomeScore = proposal.HomeScore,
			AwayScore = proposal.AwayScore
		});

		return result ?? throw new KeyNotFoundException("Match not found.");
	}

	public async Task RejectAsync(Guid matchId, Guid proposalId, string auth0Id)
	{
		var user = await GetUserAsync(auth0Id);

		var match = await _context.Matches
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.MatchId == matchId && !x.IsDeleted)
			?? throw new KeyNotFoundException("Match not found.");

		var proposal = await GetPendingProposalAsync(matchId, proposalId);

		EnsureCanReview(user, match, proposal);

		proposal.Status = PendingMatchResultStatus.Rejected;
		proposal.ReviewedByUserId = user.UserId;
		proposal.ReviewedAtUTC = DateTime.UtcNow;
		proposal.UpdatedAtUTC = DateTime.UtcNow;
		proposal.UpdatedByUserId = user.UserId;

		await _context.SaveChangesAsync();
	}

	private async Task<User> GetUserAsync(string auth0Id)
	{
		return await _context.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				x.IsActive &&
				!x.IsDeleted)
			?? throw new UnauthorizedAccessException("User not found.");
	}

	private async Task<Match> GetOpenMatchAsync(Guid matchId)
	{
		var match = await _context.Matches
			.AsNoTracking()
			.Include(x => x.Tournament)
				.ThenInclude(x => x.Season)
			.FirstOrDefaultAsync(x => x.MatchId == matchId && !x.IsDeleted)
			?? throw new KeyNotFoundException("Match not found.");

		if (match.Tournament?.Season is { IsActive: false })
			throw new UnauthorizedAccessException("This season is closed.");

		return match;
	}

	private async Task<PendingMatchResult> GetPendingProposalAsync(Guid matchId, Guid proposalId)
	{
		var proposal = await _context.PendingMatchResults
			.FirstOrDefaultAsync(x =>
				x.PendingMatchResultId == proposalId &&
				x.MatchId == matchId &&
				!x.IsDeleted)
			?? throw new KeyNotFoundException("Pending result not found.");

		if (proposal.Status != PendingMatchResultStatus.Pending)
			throw new InvalidOperationException("This result has already been reviewed.");

		return proposal;
	}

	private Task<bool> HasPendingAsync(Guid matchId)
	{
		return _context.PendingMatchResults.AnyAsync(x =>
			x.MatchId == matchId &&
			!x.IsDeleted &&
			x.Status == PendingMatchResultStatus.Pending);
	}

	private static void EnsureCanReview(User user, Match match, PendingMatchResult proposal)
	{
		if (!CanReview(user, match, proposal))
		{
			throw new UnauthorizedAccessException(
				user.UserId == proposal.SubmittedByUserId
					? "You cannot review a result you submitted."
					: "Only the opponent or an admin can review this result.");
		}
	}

	private static bool CanReview(User user, Match match, PendingMatchResult proposal)
	{
		if (user.UserId == proposal.SubmittedByUserId)
			return false;

		return IsAdmin(user) || IsParticipant(user, match);
	}

	private static bool IsAdmin(User user) =>
		string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase);

	private static bool IsParticipant(User user, Match match) =>
		user.UserId == match.HomeUserId || user.UserId == match.AwayUserId;

	private static PendingMatchResultDto ToDto(
		PendingMatchResult proposal,
		User currentUser,
		Match match,
		string submitterName)
	{
		return new PendingMatchResultDto
		{
			PendingMatchResultId = proposal.PendingMatchResultId,
			MatchId = proposal.MatchId,
			SubmittedByUserId = proposal.SubmittedByUserId,
			SubmittedByName = submitterName,
			Status = proposal.Status.ToString(),
			HomeScore = proposal.HomeScore,
			AwayScore = proposal.AwayScore,
			Summary = Deserialize<MatchSummaryStatsOcrDto>(proposal.SummaryJson),
			HomePlayers = Deserialize<TeamPerformanceOcrPreviewDto>(proposal.HomePerformanceJson)?.Players ?? [],
			AwayPlayers = Deserialize<TeamPerformanceOcrPreviewDto>(proposal.AwayPerformanceJson)?.Players ?? [],
			CreatedAtUTC = proposal.CreatedAtUTC,
			IsMine = currentUser.UserId == proposal.SubmittedByUserId,
			CanReview = CanReview(currentUser, match, proposal)
		};
	}

	private static T? Deserialize<T>(string json) where T : class
	{
		if (string.IsNullOrWhiteSpace(json))
			return null;

		try
		{
			return JsonSerializer.Deserialize<T>(json);
		}
		catch (JsonException)
		{
			return null;
		}
	}
}

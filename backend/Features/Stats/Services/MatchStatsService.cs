using backend.Data;
using backend.Entities.Enums;
using backend.Entities.MatchSummary;
using backend.Entities.Stats;
using backend.Features.Stats.Dtos;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.Stats.Services;

public class MatchStatsService : IMatchStatsService
{
	private readonly AppDbContext _context;

	public MatchStatsService(AppDbContext context)
	{
		_context = context;
	}

	public async Task SaveMatchSummaryAsync(
	ConfirmMatchSummaryDto dto)
	{
		var match = await _context.Matches
			.FirstOrDefaultAsync(x =>
				x.MatchId == dto.MatchId &&
				!x.IsDeleted);

		if (match is null)
			throw new KeyNotFoundException("Match not found.");

		var summary = await _context.MatchSummaryStats
			.FirstOrDefaultAsync(x =>
				x.MatchId == dto.MatchId);

		if (summary is null)
		{
			summary = new MatchSummaryStats
			{
				MatchSummaryStatsId = Guid.NewGuid(),
				MatchId = dto.MatchId
			};

			_context.MatchSummaryStats.Add(summary);
		}

		summary.HomePossession = dto.HomePossession;
		summary.AwayPossession = dto.AwayPossession;

		summary.HomeShots = dto.HomeShots;
		summary.AwayShots = dto.AwayShots;

		summary.HomeExpectedGoals = dto.HomeExpectedGoals;
		summary.AwayExpectedGoals = dto.AwayExpectedGoals;

		summary.HomePasses = dto.HomePasses;
		summary.AwayPasses = dto.AwayPasses;

		summary.HomeTackles = dto.HomeTackles;
		summary.AwayTackles = dto.AwayTackles;

		summary.HomeTacklesWon = dto.HomeTacklesWon;
		summary.AwayTacklesWon = dto.AwayTacklesWon;

		summary.HomeInterceptions = dto.HomeInterceptions;
		summary.AwayInterceptions = dto.AwayInterceptions;

		summary.HomeSaves = dto.HomeSaves;
		summary.AwaySaves = dto.AwaySaves;

		summary.HomeFoulsCommitted = dto.HomeFoulsCommitted;
		summary.AwayFoulsCommitted = dto.AwayFoulsCommitted;

		summary.HomeOffsides = dto.HomeOffsides;
		summary.AwayOffsides = dto.AwayOffsides;

		summary.HomeCorners = dto.HomeCorners;
		summary.AwayCorners = dto.AwayCorners;

		summary.HomeFreeKicks = dto.HomeFreeKicks;
		summary.AwayFreeKicks = dto.AwayFreeKicks;

		summary.HomePenaltyKicks = dto.HomePenaltyKicks;
		summary.AwayPenaltyKicks = dto.AwayPenaltyKicks;

		summary.HomeYellowCards = dto.HomeYellowCards;
		summary.AwayYellowCards = dto.AwayYellowCards;

		match.HomeScore = dto.HomeGoals;
		match.AwayScore = dto.AwayGoals;
		match.Status = MatchStatus.Finished;

		await _context.SaveChangesAsync();
	}

	public async Task SaveTeamPerformanceAsync(
	ConfirmTeamPerformanceDto dto)
	{
		foreach (var player in dto.Players)
		{
			_context.MatchPlayerStats.Add(new MatchPlayerStats
			{
				MatchPlayerStatsId = Guid.NewGuid(),

				MatchId = dto.MatchId,

				UserId = player.UserId,
				UserClubId = player.UserClubId,

				Position = player.Position,
				PlayerName = player.PlayerName,

				Rating = player.Rating,

				Goals = player.Goals,
				Assists = player.Assists,

				Shots = player.Shots,
				ShotAccuracy = player.ShotAccuracy,

				Passes = player.Passes,
				PassAccuracy = player.PassAccuracy,

				Dribbles = player.Dribbles,
				DribbleSuccessRate = player.DribbleSuccessRate,

				Tackles = player.Tackles,
				TackleSuccessRate = player.TackleSuccessRate,

				Offsides = player.Offsides,
				FoulsCommitted = player.FoulsCommitted,

				PossessionWon = player.PossessionWon,
				PossessionLost = player.PossessionLost,

				MinutesPlayed = player.MinutesPlayed,

				DistanceCoveredKm = player.DistanceCoveredKm,
				DistanceSprintedKm = player.DistanceSprintedKm,

				YellowCards = player.YellowCards,
				RedCards = player.RedCards,

				IsPlayerOfTheMatch = player.IsPlayerOfTheMatch
			});
		}

		await _context.SaveChangesAsync();
	}
}
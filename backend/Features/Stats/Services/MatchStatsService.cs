using backend.Data;
using backend.Entities.MatchSummary;
using backend.Entities.Stats;
using backend.Features.Stats.Dtos;

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
		var summary = new MatchSummaryStats
		{
			MatchSummaryStatsId = Guid.NewGuid(),

			MatchId = dto.MatchId,

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

			HomeCorners = dto.HomeCorners,
			AwayCorners = dto.AwayCorners,

			HomeYellowCards = dto.HomeYellowCards,
			AwayYellowCards = dto.AwayYellowCards
		};

		_context.MatchSummaryStats.Add(summary);

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
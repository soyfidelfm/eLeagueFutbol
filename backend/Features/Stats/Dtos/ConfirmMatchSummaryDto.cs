namespace backend.Features.Stats.Dtos;

public class ConfirmMatchSummaryDto
{
	public Guid MatchId { get; set; }

	public int HomePossession { get; set; }
	public int AwayPossession { get; set; }

	public int HomeShots { get; set; }
	public int AwayShots { get; set; }

	public decimal HomeExpectedGoals { get; set; }
	public decimal AwayExpectedGoals { get; set; }

	public int HomePasses { get; set; }
	public int AwayPasses { get; set; }

	public int HomeTackles { get; set; }
	public int AwayTackles { get; set; }

	public int HomeTacklesWon { get; set; }
	public int AwayTacklesWon { get; set; }

	public int HomeInterceptions { get; set; }
	public int AwayInterceptions { get; set; }

	public int HomeSaves { get; set; }
	public int AwaySaves { get; set; }

	public int HomeFoulsCommitted { get; set; }
	public int AwayFoulsCommitted { get; set; }

	public int HomeOffsides { get; set; }
	public int AwayOffsides { get; set; }

	public int HomeCorners { get; set; }
	public int AwayCorners { get; set; }

	public int HomeFreeKicks { get; set; }
	public int AwayFreeKicks { get; set; }

	public int HomePenaltyKicks { get; set; }
	public int AwayPenaltyKicks { get; set; }

	public int HomeYellowCards { get; set; }
	public int AwayYellowCards { get; set; }
}
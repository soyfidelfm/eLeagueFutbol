namespace backend.Entities.Transfers;

using backend.Entities.Clubs;
using backend.Entities.Common;
using backend.Entities.Tournaments;

public class UserClubBudgetTransaction : BaseEntity
{
	public Guid UserClubBudgetTransactionId { get; set; }

	public Guid UserClubId { get; set; }

	public Guid SeasonId { get; set; }

	public decimal Amount { get; set; }

	public string Type { get; set; } = string.Empty;
	// InitialBudget
	// DraftPurchase
	// TransferIn
	// TransferOut
	// MatchReward
	// AdminAdjustment

	public string Description { get; set; } = string.Empty;

	public UserClub UserClub { get; set; } = null!;

	public Season Season { get; set; } = null!;
}
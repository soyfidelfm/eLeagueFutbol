using backend.Entities.Common;
using backend.Entities.GameData;
using backend.Entities.Identity;
using backend.Entities.Tournaments;
using backend.Entities.Transfers;

namespace backend.Entities.Clubs
{
	public class UserClub : BaseEntity
	{
		public Guid UserClubId { get; set; }

		public Guid UserId { get; set; }

		public Guid SeasonId { get; set; }

		public Guid? BaseGameClubId { get; set; }

		public string Name { get; set; } = string.Empty;

		public string LogoUrl { get; set; } = string.Empty;

		public decimal InitialBudget { get; set; }

		public decimal CurrentBudget { get; set; }

		public User User { get; set; } = null!;

		public Season Season { get; set; } = null!;

		public GameClub? BaseGameClub { get; set; }

		public ICollection<UserClubPlayer> Players { get; set; } = [];

		public ICollection<ClubLineup> Lineups { get; set; } = [];
		public ICollection<TransferRecord> TransfersIn { get; set; } = [];

		public ICollection<TransferRecord> TransfersOut { get; set; } = [];

		public ICollection<UserClubBudgetTransaction> BudgetTransactions { get; set; } = [];
	}
}

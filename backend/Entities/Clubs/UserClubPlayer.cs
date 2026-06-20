namespace backend.Entities.Clubs;

using backend.Entities.Common;
using backend.Entities.GameData;

public class UserClubPlayer : BaseEntity
{
	public Guid UserClubPlayerId { get; set; }

	public Guid UserClubId { get; set; }

	public Guid GamePlayerId { get; set; }

	public decimal AcquisitionPrice { get; set; }

	public DateTime AcquiredAtUTC { get; set; }

	public string AcquisitionType { get; set; } = "Draft";

	public UserClub UserClub { get; set; } = null!;

	public GamePlayer GamePlayer { get; set; } = null!;
	public ICollection<ClubLineupPlayer> LineupPlayers { get; set; } = [];
}
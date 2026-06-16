using backend.Entities.GameData;
using backend.Entities.Identity;
using backend.Entities.Tournaments;

public class DraftPick
{
	public Guid DraftPickId { get; set; }

	public Guid SeasonId { get; set; }
	public Season Season { get; set; } = null!;

	public Guid UserId { get; set; }
	public User User { get; set; } = null!;

	public Guid GamePlayerId { get; set; }
	public GamePlayer GamePlayer { get; set; } = null!;

	public int PickNumber { get; set; }

	public int RoundNumber { get; set; }

	public DateTime PickedAtUTC { get; set; }
}
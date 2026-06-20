using backend.Entities.Clubs;
using backend.Entities.Common;
using backend.Entities.Draft;
using backend.Entities.Transfers;

namespace backend.Entities.GameData;

public class GamePlayer : BaseEntity
{
	public Guid GamePlayerId { get; set; }

	// SoFIFA / FC26 Player ID
	public int SofifaPlayerId { get; set; }

	// Opcional por si después vuelves a usar Futbin
	public int? FutbinId { get; set; }

	public string Name { get; set; } = string.Empty;

	public string FullName { get; set; } = string.Empty;

	public int Overall { get; set; }

	public int Potential { get; set; }

	public string Position { get; set; } = string.Empty;

	public int? ValueEur { get; set; }

	public int? WageEur { get; set; }

	public int? ReleaseClauseEur { get; set; }

	public int Age { get; set; }

	public DateTime? DateOfBirth { get; set; }

	public int HeightCm { get; set; }

	public int WeightKg { get; set; }

	public int? ClubTeamId { get; set; }

	public string? ClubName { get; set; }

	public int? ClubJerseyNumber { get; set; }

	public int? ClubContractValidUntilYear { get; set; }

	public int? NationalityId { get; set; }

	public string Nation { get; set; } = string.Empty;

	public int? NationTeamId { get; set; }

	public string PreferredFoot { get; set; } = string.Empty;

	public int? WeakFoot { get; set; }

	public int? SkillMoves { get; set; }

	public int? Pace { get; set; }

	public int? Shooting { get; set; }

	public int? Passing { get; set; }

	public int? Dribbling { get; set; }

	public int? Defending { get; set; }

	public int? Physic { get; set; }

	public string? ImageUrl { get; set; }

	public bool IsActive { get; set; } = true;

	// Foreign Key
	public Guid GameClubId { get; set; }

	// Navigation Property
	public GameClub GameClub { get; set; } = null!;

	public ICollection<DraftPick> DraftPicks { get; set; } = [];
	public ICollection<UserClubPlayer> UserClubPlayers { get; set; } = [];

	public ICollection<TransferRecord> TransferRecords { get; set; } = [];
}
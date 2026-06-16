using backend.Entities.Common;
using backend.Entities.Draft;

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

	public string Nation { get; set; } = string.Empty;

	public int Age { get; set; }

	public int HeightCm { get; set; }

	public int WeightKg { get; set; }

	public string PreferredFoot { get; set; } = string.Empty;

	public string? ImageUrl { get; set; }

	public bool IsActive { get; set; } = true;

	// Foreign Key
	public Guid GameClubId { get; set; }

	// Navigation Property
	public GameClub GameClub { get; set; } = null!;
	public ICollection<DraftPick> DraftPicks { get; set; } = [];
}
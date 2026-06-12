using backend.Entities.Common;

namespace backend.Entities.GameData;

public class GameClub : BaseEntity
{
	public Guid GameClubId { get; set; }

	public int ExternalTeamId { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Slug { get; set; } = string.Empty;

	public string Code { get; set; } = string.Empty;

	public string League { get; set; } = string.Empty;

	public string Country { get; set; } = string.Empty;

	public string StadiumName { get; set; } = string.Empty;

	public int OverallRating { get; set; }

	public int AttackRating { get; set; }

	public int MidfieldRating { get; set; }

	public int DefenseRating { get; set; }

	public bool IsActive { get; set; } = true;
	public string? LogoUrl { get; set; }
	public string? LogoPublicId { get; set; }

	public ICollection<GamePlayer> Players { get; set; }
		= new List<GamePlayer>();
}
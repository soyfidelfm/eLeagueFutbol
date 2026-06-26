using backend.Entities.Clubs;

namespace backend.Entities.Draft;

public class DraftPickAsset
{
	public Guid DraftPickAssetId { get; set; }

	public Guid SeasonId { get; set; }

	public int RoundNumber { get; set; }
	public int PickNumber { get; set; }

	public Guid OriginalUserClubId { get; set; }
	public UserClub OriginalUserClub { get; set; } = null!;

	public Guid CurrentOwnerUserClubId { get; set; }
	public UserClub CurrentOwnerUserClub { get; set; } = null!;

	public bool IsUsed { get; set; }

	public DateTime CreatedAtUTC { get; set; }
	public DateTime? UpdatedAtUTC { get; set; }
}
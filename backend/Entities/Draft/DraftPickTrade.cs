using backend.Entities.Clubs;

namespace backend.Entities.Draft;

public class DraftPickTrade
{
	public Guid DraftPickTradeId { get; set; }

	public Guid SeasonId { get; set; }

	public Guid FromUserClubId { get; set; }
	public UserClub FromUserClub { get; set; } = null!;

	public Guid ToUserClubId { get; set; }
	public UserClub ToUserClub { get; set; } = null!;

	public string Status { get; set; } = "Pending";
	public string Message { get; set; } = string.Empty;

	public DateTime CreatedAtUTC { get; set; }
	public DateTime? RespondedAtUTC { get; set; }

	public ICollection<DraftPickTradeAsset> TradeAssets { get; set; } = [];
}
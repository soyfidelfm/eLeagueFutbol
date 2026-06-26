namespace backend.Entities.Draft;

public class DraftPickTradeAsset
{
	public Guid DraftPickTradeAssetId { get; set; }

	public Guid DraftPickTradeId { get; set; }
	public DraftPickTrade DraftPickTrade { get; set; } = null!;

	public Guid DraftPickAssetId { get; set; }
	public DraftPickAsset DraftPickAsset { get; set; } = null!;

	public DateTime CreatedAtUTC { get; set; }
}
namespace backend.Features.Draft.Dtos;

public class ProposeDraftPickTradeDto
{
	public Guid ToUserClubId { get; set; }

	public List<Guid> DraftPickAssetIds { get; set; } = [];

	public string? Message { get; set; }
}
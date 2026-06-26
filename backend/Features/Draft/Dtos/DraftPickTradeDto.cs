namespace backend.Features.Draft.Dtos;

public class DraftPickTradeDto
{
	public Guid DraftPickTradeId { get; set; }

	public Guid SeasonId { get; set; }

	public Guid FromUserClubId { get; set; }
	public string FromClubName { get; set; } = string.Empty;

	public Guid ToUserClubId { get; set; }
	public string ToClubName { get; set; } = string.Empty;

	public string Status { get; set; } = string.Empty;
	public string Message { get; set; } = string.Empty;

	public DateTime CreatedAtUTC { get; set; }
	public DateTime? RespondedAtUTC { get; set; }

	public List<DraftPickAssetDto> Picks { get; set; } = [];
}
namespace backend.Features.Draft.Dtos;

public class DraftLotteryPreviewDto
{
	public int DrawNumber { get; set; }

	public bool IsFinal { get; set; }

	public List<DraftOrderDto> DraftOrder { get; set; } = [];
}
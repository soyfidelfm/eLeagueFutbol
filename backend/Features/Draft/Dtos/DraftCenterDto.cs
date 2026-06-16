namespace backend.Features.Draft.Dtos;

public class DraftCenterDto
{
	public Guid MyUserId { get; set; }
	public DraftStateDto State { get; set; } = new();

	public List<DraftOrderDto> DraftOrder { get; set; } = [];

	public List<DraftPickDto> RecentPicks { get; set; } = [];

	public MyDraftSquadDto MySquad { get; set; } = new();
}
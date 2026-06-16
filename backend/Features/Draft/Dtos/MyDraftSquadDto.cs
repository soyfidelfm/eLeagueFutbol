namespace backend.Features.Draft.Dtos;

public class MyDraftSquadDto
{
	public int TotalPlayers { get; set; }

	public List<DraftSquadPlayerDto> Players { get; set; } = [];
}
namespace backend.Features.Matches.Dtos;

public sealed class MatchPlayerOptionsDto
{
	public List<TeamPlayerOptionDto> HomeTeamPlayers { get; set; } = [];

	public List<TeamPlayerOptionDto> AwayTeamPlayers { get; set; } = [];
}
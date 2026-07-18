namespace backend.Features.Matches.Dtos;

public sealed class TeamPlayerOptionDto
{
	public Guid GamePlayerId { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Position { get; set; } = string.Empty;

	public int Overall { get; set; }

	public string? ImageUrl { get; set; }
}
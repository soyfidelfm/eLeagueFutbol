namespace backend.Features.Draft.Dtos;

public class DraftPlayerDto
{
	public Guid GamePlayerId { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Position { get; set; } = string.Empty;

	public int Overall { get; set; }

	public int Potential { get; set; }

	public int Pace { get; set; }

	public int Shooting { get; set; }

	public int Passing { get; set; }

	public int Dribbling { get; set; }

	public int Defending { get; set; }

	public int Physic { get; set; }

	public decimal Value { get; set; }

	public string Nation { get; set; } = string.Empty;

	public int Age { get; set; }

	public string? ImageUrl { get; set; }

	public string GameClubName { get; set; } = string.Empty;
}
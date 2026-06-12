namespace backend.Features.GameData.Dtos;

public class GamePlayerDto
{
	public Guid GamePlayerId { get; set; }

	public int SofifaPlayerId { get; set; }

	public int? FutbinId { get; set; }

	public string Name { get; set; } = string.Empty;

	public string FullName { get; set; } = string.Empty;

	public int Overall { get; set; }

	public int Potential { get; set; }

	public string Position { get; set; } = string.Empty;

	public string Nation { get; set; } = string.Empty;

	public int Age { get; set; }

	public int HeightCm { get; set; }

	public int WeightKg { get; set; }

	public string PreferredFoot { get; set; } = string.Empty;

	public string? ImageUrl { get; set; }

	public Guid GameClubId { get; set; }

	public string GameClubName { get; set; } = string.Empty;
}
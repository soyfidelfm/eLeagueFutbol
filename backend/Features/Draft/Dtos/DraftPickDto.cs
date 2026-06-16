namespace backend.Features.Draft.Dtos;

public class DraftPickDto
{
	public int PickNumber { get; set; }

	public int RoundNumber { get; set; }

	public Guid UserId { get; set; }

	public string ClubName { get; set; } = string.Empty;

	public Guid GamePlayerId { get; set; }

	public string PlayerName { get; set; } = string.Empty;

	public string Position { get; set; } = string.Empty;

	public int Overall { get; set; }

	public string? PlayerImageUrl { get; set; }

	public DateTime PickedAtUTC { get; set; }
}
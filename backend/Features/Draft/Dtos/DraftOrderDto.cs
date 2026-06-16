namespace backend.Features.Draft.Dtos;

public class DraftOrderDto
{
	public int PickOrder { get; set; }

	public Guid UserId { get; set; }

	public string ClubName { get; set; } = string.Empty;

	public string? LogoUrl { get; set; }

	public bool IsCurrentPick { get; set; }
}
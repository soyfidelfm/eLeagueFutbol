namespace backend.Features.Draft.Dtos;

public class DraftPickAssetDto
{
	public Guid DraftPickAssetId { get; set; }

	public Guid SeasonId { get; set; }

	public int RoundNumber { get; set; }
	public int PickNumber { get; set; }

	public Guid OriginalUserClubId { get; set; }
	public string OriginalClubName { get; set; } = string.Empty;

	public Guid CurrentOwnerUserClubId { get; set; }
	public string CurrentOwnerClubName { get; set; } = string.Empty;

	public bool IsUsed { get; set; }
}
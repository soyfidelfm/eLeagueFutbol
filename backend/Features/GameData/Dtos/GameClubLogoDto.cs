namespace backend.Features.GameData.Dtos
{
	public class GameClubLogoDto
	{
		public Guid GameClubId { get; set; }
		public string Name { get; set; } = string.Empty;
		public string LogoUrl { get; set; } = string.Empty;
		public string LogoPublicId { get; set; } = string.Empty;
	}
}

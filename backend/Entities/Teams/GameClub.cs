using backend.Entities.Common;

namespace backend.Entities.Teams
{
	public class GameClub : BaseEntity
	{
		public Guid GameClubId { get; set; }

		public string Name { get; set; } = string.Empty;

		public string Code { get; set; } = string.Empty;

		public string League { get; set; } = string.Empty;

		public string Country { get; set; } = string.Empty;

		public string LogoUrl { get; set; } = string.Empty;

		public bool IsActive { get; set; } = true;
	}
}

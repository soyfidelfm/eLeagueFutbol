using backend.Entities.Clubs;
using backend.Entities.Common;

namespace backend.Entities.GameData
{
	public class ClubLineup : BaseEntity
	{
		public Guid ClubLineupId { get; set; }

		public Guid UserClubId { get; set; }

		public UserClub UserClub { get; set; } = null!;

		public string Formation { get; set; } = string.Empty;

		public List<ClubLineupPlayer> Players { get; set; } = [];
	}
}

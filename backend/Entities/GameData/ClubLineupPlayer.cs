using backend.Entities.Clubs;
using backend.Entities.Common;

namespace backend.Entities.GameData
{
	public class ClubLineupPlayer : BaseEntity
	{
		public Guid ClubLineupPlayerId { get; set; }

		public Guid ClubLineupId { get; set; }

		public Guid UserClubPlayerId { get; set; }

		public UserClubPlayer UserClubPlayer { get; set; } = null!;

		public int SlotIndex { get; set; }

		public string Position { get; set; } = string.Empty;

		public ClubLineup ClubLineup { get; set; } = null!;
	}
}

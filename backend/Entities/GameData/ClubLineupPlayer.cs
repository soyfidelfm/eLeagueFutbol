namespace backend.Entities.GameData
{
	public class ClubLineupPlayer
	{
		public Guid ClubLineupPlayerId { get; set; }

		public Guid ClubLineupId { get; set; }

		public Guid GamePlayerId { get; set; }

		public int SlotIndex { get; set; }

		public string Position { get; set; } = string.Empty;

		public ClubLineup ClubLineup { get; set; } = null!;
	}
}

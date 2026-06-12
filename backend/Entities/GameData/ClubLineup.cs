namespace backend.Entities.GameData
{
	public class ClubLineup
	{
		public Guid ClubLineupId { get; set; }

		public Guid UserId { get; set; }

		public Guid GameClubId { get; set; }

		public string Formation { get; set; } = string.Empty;

		public DateTime CreatedAtUTC { get; set; }

		public DateTime UpdatedAtUTC { get; set; }

		public List<ClubLineupPlayer> Players { get; set; } = [];
	}
}

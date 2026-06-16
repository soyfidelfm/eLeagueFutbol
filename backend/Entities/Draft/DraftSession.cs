namespace backend.Entities.Draft
{
	public class DraftSession
{
	public Guid DraftSessionId { get; set; }

	public Guid SeasonId { get; set; }

	public bool IsActive { get; set; }

	public int CurrentPickNumber { get; set; }

	public int PlayersPerTeam { get; set; }

	public DateTime CreatedAtUTC { get; set; }
}
}
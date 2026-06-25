using backend.Entities.Common;
using backend.Entities.Identity;
using backend.Entities.Tournaments;

public class DraftOrder : BaseEntity
{
	public Guid DraftOrderId { get; set; }

	public Guid SeasonId { get; set; }
	public Season Season { get; set; } = null!;

	public Guid UserId { get; set; }
	public User User { get; set; } = null!;

	public int PickOrder { get; set; }
}
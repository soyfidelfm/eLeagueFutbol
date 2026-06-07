using backend.Entities.Identity;
using backend.Entities.Teams;
using backend.Entities.Tournaments;

public class SeasonRegistration
{
	public Guid SeasonRegistrationId { get; set; }

	public Guid SeasonId { get; set; }
	public Guid UserId { get; set; }
	public Guid GameClubId { get; set; }

	public DateTime RegisteredAtUTC { get; set; } = DateTime.UtcNow;

	public Season Season { get; set; } = null!;
	public User User { get; set; } = null!;
	public GameClub GameClub { get; set; } = null!;
}
namespace backend.Entities.Identity;

using backend.Entities.Clubs;
using backend.Entities.Common;
using backend.Entities.Draft;
using backend.Entities.GameData;
using backend.Entities.Notifications;
using backend.Entities.Stats;
using backend.Entities.Tournaments;

public class User : BaseEntity
{
	public Guid UserId { get; set; }

	public string Auth0Id { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public string DisplayName { get; set; } = string.Empty;

	public string AvatarUrl { get; set; } = string.Empty;

	public string TimeZone { get; set; } = "UTC";

	public DateTime? LastLoginAtUTC { get; set; }

	public bool IsActive { get; set; } = true;

	public string ClubName { get; set; } = string.Empty;

	public Guid? GameClubId { get; set; }

	public GameClub? GameClub { get; set; }

	public string GamerTag { get; set; } = string.Empty;

	public string Role { get; set; } = string.Empty;

	public ICollection<MatchPlayerStats> MatchStats { get; set; }
		= new List<MatchPlayerStats>();

	public ICollection<Match> HomeMatches { get; set; }
		= new List<Match>();

	public ICollection<Match> AwayMatches { get; set; }
		= new List<Match>();

	public ICollection<DraftOrder> DraftOrders { get; set; } = [];
	public ICollection<DraftPick> DraftPicks { get; set; } = [];
	public ICollection<NotificationToken> NotificationTokens { get; set; }
		= new List<NotificationToken>();
	public ICollection<UserClub> UserClubs { get; set; } = [];
}

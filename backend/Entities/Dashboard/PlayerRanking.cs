using backend.Entities.Identity;

namespace backend.Entities.Dashboard;

public class PlayerRanking
{
	public Guid Id { get; set; }

	public Guid UserId { get; set; }

	public int Points { get; set; }

	public int Wins { get; set; }

	public int Losses { get; set; }

	public int GoalDifference { get; set; }

	public string Form { get; set; } = string.Empty;

	public DateTime CreatedAtUTC { get; set; }

	public User User { get; set; } = null!;
}
namespace backend.Features.Draft.Dtos;

public class DraftStateDto
{
	public Guid DraftSessionId { get; set; }

	public bool IsActive { get; set; }

	public int CurrentRound { get; set; }

	public int CurrentPickNumber { get; set; }

	public Guid? CurrentUserId { get; set; }

	public string CurrentClubName { get; set; } = string.Empty;

	public int SecondsRemaining { get; set; }

	public int TotalTeams { get; set; }

	public int PlayersPerTeam { get; set; }

	public bool IsPaused { get; set; }

	public bool IsCompleted { get; set; }

	public bool TimerEnabled { get; set; }

	public int PickSeconds { get; set; }

	/// <summary>When the current pick expires (UTC). Null when paused, timer off or inactive.</summary>
	public DateTime? CurrentPickDeadlineUTC { get; set; }

	/// <summary>Server clock when the state was built, so clients can correct their own clock skew.</summary>
	public DateTime ServerTimeUTC { get; set; }
}
namespace backend.Entities.Draft
{
	public class DraftSession
	{
		public Guid DraftSessionId { get; set; }

		public Guid SeasonId { get; set; }

		public bool IsActive { get; set; }

		public int CurrentPickNumber { get; set; }

		public int PlayersPerTeam { get; set; }

		public DateTime? CurrentPickStartedAtUTC { get; set; }

		public bool OneMinuteWarningSent { get; set; }

		/// <summary>True while an admin has paused an active draft. The clock is frozen.</summary>
		public bool IsPaused { get; set; }

		/// <summary>True once every pick was made or an admin stopped the draft. It cannot be restarted.</summary>
		public bool IsCompleted { get; set; }

		/// <summary>When false, picks never expire and nobody is skipped.</summary>
		public bool TimerEnabled { get; set; } = true;

		/// <summary>Seconds each team has for a pick.</summary>
		public int PickSeconds { get; set; } = 300;

		/// <summary>Server-authoritative moment the current pick expires (null when paused, timer off or inactive).</summary>
		public DateTime? CurrentPickDeadlineUTC { get; set; }

		/// <summary>Seconds left on the current pick when the draft was paused.</summary>
		public int? PausedRemainingSeconds { get; set; }

		public DateTime CreatedAtUTC { get; set; }
	}
}
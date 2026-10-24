namespace backend.Features.Draft.Dtos;

/// <summary>Admin change to the per-pick timer of a season's draft.</summary>
public class UpdateDraftTimerDto
{
	public bool TimerEnabled { get; set; }

	public int PickSeconds { get; set; }
}

/// <summary>League-wide draft defaults edited from Settings (admins only).</summary>
public class DraftSettingsDto
{
	public int DefaultPickSeconds { get; set; }

	public int MinPickSeconds { get; set; } = DraftTimerLimits.MinPickSeconds;

	public int MaxPickSeconds { get; set; } = DraftTimerLimits.MaxPickSeconds;
}

public static class DraftTimerLimits
{
	public const int MinPickSeconds = 10;
	public const int MaxPickSeconds = 600;
	public const int DefaultPickSeconds = 300;
	public const string DefaultPickSecondsKey = "Draft.DefaultPickSeconds";
}

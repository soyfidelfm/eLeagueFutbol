namespace backend.Features.Users.Dtos;

public class UserSettingsDto
{
	/// <summary>IANA time zone id, e.g. "America/Los_Angeles".</summary>
	public string TimeZone { get; set; } = "UTC";

	/// <summary>"en" or "es".</summary>
	public string Language { get; set; } = "en";

	public bool PushNotificationsEnabled { get; set; } = true;

	public bool InAppToastsEnabled { get; set; } = true;
}

namespace backend.Features.Notifications.Dtos;

public class SaveNotificationTokenDto
{
	public string Token { get; set; } = string.Empty;

	public string DeviceType { get; set; } = "web";
}
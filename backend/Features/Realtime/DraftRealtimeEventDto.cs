namespace backend.Features.Realtime;

public class DraftRealtimeEventDto
{
	public Guid SeasonId { get; set; }

	public string EventType { get; set; } = string.Empty;
}

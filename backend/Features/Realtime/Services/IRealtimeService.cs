namespace backend.Features.Realtime.Services;

public interface IRealtimeService
{
	Task SendToUserAsync(
		Guid userId,
		string eventName,
		object payload);
}
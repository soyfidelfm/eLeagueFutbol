namespace backend.Features.Draft.Services;

using backend.Features.Realtime;

internal static class DraftRealtimePublisher
{
	public static Task LotteryAsync(IAppRealtimeService realtime, Guid seasonId, int drawNumber)
	{
		return realtime.PublishDraftEventAsync(
			seasonId,
			drawNumber == 3 ? "lotteryFinal" : "lottery");
	}

	public static Task StartedAsync(IAppRealtimeService realtime, Guid seasonId)
	{
		return realtime.PublishDraftEventAsync(seasonId, "started");
	}

	public static Task OrderGeneratedAsync(IAppRealtimeService realtime, Guid seasonId)
	{
		return realtime.PublishDraftEventAsync(seasonId, "orderGenerated");
	}

	public static Task PickMadeAsync(IAppRealtimeService realtime, Guid seasonId)
	{
		return realtime.PublishDraftEventAsync(seasonId, "pickMade");
	}
}

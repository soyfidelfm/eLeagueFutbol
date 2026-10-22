namespace backend.Features.Draft.Services;

using backend.Features.Realtime;

/// <summary>
/// Server-side pick clock: every couple of seconds it skips picks whose deadline passed and
/// broadcasts a DraftEvent, so the draft keeps moving even when nobody has the page open.
/// </summary>
public class DraftTimerBackgroundService : BackgroundService
{
	private static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);

	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<DraftTimerBackgroundService> _logger;

	public DraftTimerBackgroundService(
		IServiceScopeFactory scopeFactory,
		ILogger<DraftTimerBackgroundService> logger)
	{
		_scopeFactory = scopeFactory;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var timer = new PeriodicTimer(Interval);

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await using var scope = _scopeFactory.CreateAsyncScope();

				var draft = scope.ServiceProvider.GetRequiredService<DraftService>();
				var realtime = scope.ServiceProvider.GetRequiredService<IAppRealtimeService>();

				var outcomes = await draft.ProcessExpiredPicksAsync(stoppingToken);

				foreach (var outcome in outcomes)
					await realtime.PublishDraftEventAsync(outcome.SeasonId, outcome.EventType);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Draft timer sweep failed.");
			}

			try
			{
				if (!await timer.WaitForNextTickAsync(stoppingToken))
					break;
			}
			catch (OperationCanceledException)
			{
				break;
			}
		}
	}
}

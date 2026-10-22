namespace backend.Features.Draft.Services;

using backend.Entities.Enums;
using backend.Features.Draft.Dtos;
using backend.Features.Realtime;

public class DraftRealtimeDecorator : IDraftService
{
	private readonly DraftService _inner;
	private readonly IAppRealtimeService _realtime;

	public DraftRealtimeDecorator(DraftService inner, IAppRealtimeService realtime)
	{
		_inner = inner;
		_realtime = realtime;
	}

	public Task<DraftCenterDto?> GetCenterAsync(Guid seasonId, string auth0Id)
	{
		return _inner.GetCenterAsync(seasonId, auth0Id);
	}

	public async Task<DraftCenterDto> GenerateDraftOrderAsync(
		Guid seasonId,
		string auth0Id,
		StartDraftDto dto)
	{
		var result = await _inner.GenerateDraftOrderAsync(seasonId, auth0Id, dto);
		await _realtime.PublishDraftEventAsync(seasonId, "orderGenerated");
		return result;
	}

	public async Task<DraftCenterDto> StartDraftAsync(Guid seasonId, string auth0Id)
	{
		var result = await _inner.StartDraftAsync(seasonId, auth0Id);
		await _realtime.PublishDraftEventAsync(seasonId, "started");
		return result;
	}

	public Task<List<DraftPlayerDto>> GetAvailablePlayersAsync(
		Guid seasonId,
		string? search,
		string? position,
		int page,
		int pageSize,
		DraftPlayerPoolFilter poolFilter = DraftPlayerPoolFilter.ClubOnly)
	{
		return _inner.GetAvailablePlayersAsync(
			seasonId,
			search,
			position,
			page,
			pageSize,
			poolFilter);
	}

	public Task<List<DraftPickDto>> GetPicksAsync(Guid seasonId)
	{
		return _inner.GetPicksAsync(seasonId);
	}

	public async Task<DraftPickDto> MakePickAsync(
		Guid seasonId,
		string auth0Id,
		MakeDraftPickDto dto)
	{
		var result = await _inner.MakePickAsync(seasonId, auth0Id, dto);
		await _realtime.PublishDraftEventAsync(seasonId, "pickMade");
		return result;
	}

	public async Task<DraftLotteryPreviewDto> PreviewDraftOrderAsync(
		Guid seasonId,
		int drawNumber,
		string auth0Id)
	{
		var result = await _inner.PreviewDraftOrderAsync(seasonId, drawNumber, auth0Id);
		await _realtime.PublishDraftEventAsync(
			seasonId,
			drawNumber == 3 ? "lotteryFinal" : "lottery");
		return result;
	}

	public async Task<DraftCenterDto> PauseDraftAsync(Guid seasonId, string auth0Id)
	{
		var result = await _inner.PauseDraftAsync(seasonId, auth0Id);
		await _realtime.PublishDraftEventAsync(seasonId, "paused");
		return result;
	}

	public async Task<DraftCenterDto> ResumeDraftAsync(Guid seasonId, string auth0Id)
	{
		var result = await _inner.ResumeDraftAsync(seasonId, auth0Id);
		await _realtime.PublishDraftEventAsync(seasonId, "resumed");
		return result;
	}

	public async Task<DraftCenterDto> StopDraftAsync(Guid seasonId, string auth0Id)
	{
		var result = await _inner.StopDraftAsync(seasonId, auth0Id);
		await _realtime.PublishDraftEventAsync(seasonId, "stopped");
		return result;
	}

	public async Task<DraftCenterDto> UpdateTimerAsync(Guid seasonId, string auth0Id, UpdateDraftTimerDto dto)
	{
		var result = await _inner.UpdateTimerAsync(seasonId, auth0Id, dto);
		await _realtime.PublishDraftEventAsync(seasonId, "timerUpdated");
		return result;
	}

	public Task<DraftSettingsDto> GetDraftSettingsAsync()
	{
		return _inner.GetDraftSettingsAsync();
	}

	public Task<DraftSettingsDto> UpdateDraftSettingsAsync(string auth0Id, DraftSettingsDto dto)
	{
		return _inner.UpdateDraftSettingsAsync(auth0Id, dto);
	}
}

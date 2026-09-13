namespace backend.Features.Transfers.Services;

using backend.Data;
using backend.Features.Realtime;
using backend.Features.Transfers.Dtos;
using Microsoft.EntityFrameworkCore;

public class TransferRealtimeDecorator : ITransferService
{
	private readonly TransferService _inner;
	private readonly IAppRealtimeService _realtime;
	private readonly AppDbContext _db;

	public TransferRealtimeDecorator(
		TransferService inner,
		IAppRealtimeService realtime,
		AppDbContext db)
	{
		_inner = inner;
		_realtime = realtime;
		_db = db;
	}

	public Task<TransferBoardDto> GetBoardAsync(string auth0Id, Guid seasonId)
	{
		return _inner.GetBoardAsync(auth0Id, seasonId);
	}

	public async Task<TransferOfferDto> CreateOfferAsync(
		string auth0Id,
		Guid seasonId,
		CreateTransferOfferDto dto)
	{
		await EnsureSeasonPlayableAsync(seasonId);

		var result = await _inner.CreateOfferAsync(auth0Id, seasonId, dto);
		await _realtime.PublishDraftEventAsync(seasonId, "transferOffer");
		return result;
	}

	public async Task<TransferOfferDto> AcceptOfferAsync(string auth0Id, Guid transferRecordId)
	{
		var seasonId = await GetSeasonIdAsync(transferRecordId);
		await EnsureSeasonPlayableAsync(seasonId);
		return await _inner.AcceptOfferAsync(auth0Id, transferRecordId);
	}

	public async Task<TransferOfferDto> RejectOfferAsync(string auth0Id, Guid transferRecordId)
	{
		var seasonId = await GetSeasonIdAsync(transferRecordId);
		var result = await _inner.RejectOfferAsync(auth0Id, transferRecordId);
		await _realtime.PublishDraftEventAsync(seasonId, "transferRejected");
		return result;
	}

	public async Task CancelOfferAsync(string auth0Id, Guid transferRecordId)
	{
		var seasonId = await GetSeasonIdAsync(transferRecordId);
		await _inner.CancelOfferAsync(auth0Id, transferRecordId);
		await _realtime.PublishDraftEventAsync(seasonId, "transferCancelled");
	}

	private async Task EnsureSeasonPlayableAsync(Guid seasonId)
	{
		var season = await _db.Seasons
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.SeasonId == seasonId && !x.IsDeleted)
			?? throw new InvalidOperationException("Season not found.");

		if (!season.IsActive)
			throw new InvalidOperationException("This season is closed.");
	}

	private async Task<Guid> GetSeasonIdAsync(Guid transferRecordId)
	{
		var seasonId = await _db.TransferRecords
			.AsNoTracking()
			.Where(x => x.TransferRecordId == transferRecordId && !x.IsDeleted)
			.Select(x => (Guid?)x.SeasonId)
			.FirstOrDefaultAsync();

		if (!seasonId.HasValue)
			throw new InvalidOperationException("Offer not found.");

		return seasonId.Value;
	}
}

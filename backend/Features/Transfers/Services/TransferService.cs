namespace backend.Features.Transfers.Services;

using backend.Data;
using backend.Entities.Clubs;
using backend.Entities.Notifications;
using backend.Entities.Transfers;
using backend.Features.Notifications.Dtos;
using backend.Features.Notifications.Services;
using backend.Features.Realtime;
using backend.Features.Transfers.Dtos;
using Microsoft.EntityFrameworkCore;

public class TransferService : ITransferService
{
	public const string OfferPending = "OfferPending";
	public const string OfferRejected = "OfferRejected";
	public const string OfferCancelled = "OfferCancelled";
	public const string TransferCompleted = "Transfer";

	private readonly AppDbContext _context;
	private readonly IAppRealtimeService _realtime;
	private readonly INotificationService _push;

	public TransferService(
		AppDbContext context,
		IAppRealtimeService realtime,
		INotificationService push)
	{
		_context = context;
		_realtime = realtime;
		_push = push;
	}

	public async Task<TransferBoardDto> GetBoardAsync(string auth0Id, Guid seasonId)
	{
		var me = await GetUserAsync(auth0Id);
		var myClub = await GetMyClubAsync(me.UserId, seasonId);

		var pendingMine = myClub is null
			? []
			: await _context.TransferRecords
				.AsNoTracking()
				.Where(x =>
					x.SeasonId == seasonId &&
					!x.IsDeleted &&
					x.TransferType == OfferPending &&
					x.ToUserClubId == myClub.UserClubId)
				.Select(x => x.GamePlayerId)
				.ToListAsync();

		var marketQuery = _context.UserClubPlayers
			.AsNoTracking()
			.Where(x =>
				!x.IsDeleted &&
				!x.UserClub.IsDeleted &&
				x.UserClub.SeasonId == seasonId);

		if (myClub is not null)
		{
			marketQuery = marketQuery.Where(x => x.UserClubId != myClub.UserClubId);
		}

		var market = await marketQuery
			.OrderByDescending(x => x.GamePlayer.Overall)
			.Select(x => new TransferMarketPlayerDto
			{
				GamePlayerId = x.GamePlayerId,
				OwnerUserClubId = x.UserClubId,
				OwnerUserId = x.UserClub.UserId,
				OwnerClubName = x.UserClub.Name,
				Name = x.GamePlayer.Name,
				Position = x.GamePlayer.Position,
				Overall = x.GamePlayer.Overall,
				MarketValue = x.GamePlayer.ValueEur,
				ImageUrl = x.GamePlayer.ImageUrl,
				HasMyPendingOffer = pendingMine.Contains(x.GamePlayerId)
			})
			.Take(200)
			.ToListAsync();

		var offers = myClub is null
			? []
			: await QueryOffers(seasonId)
				.Where(x =>
					x.FromUserClubId == myClub.UserClubId ||
					x.ToUserClubId == myClub.UserClubId)
				.ToListAsync();

		foreach (var offer in offers)
		{
			offfer.IsIncoming = myClub is not null && offer.FromUserClubId == myClub.UserClubId;
		}

		return new TransferBoardDto
		{
			SeasonId = seasonId,
			MyUserClubId = myClub?.UserClubId,
			MyClubName = myClub?.Name ?? string.Empty,
			CurrentBudget = myClub?.CurrentBudget ?? 0,
			Market = market,
			IncomingOffers = offers.Where(x => x.TransferType == OfferPending && x.IsIncoming).ToList(),
			OutgoingOffers = offers.Where(x => x.TransferType == OfferPending && !x.IsIncoming).ToList(),
			History = offers.Where(x => x.TransferType == TransferCompleted).ToList()
		};
	}

	public async Task<TransferOfferDto> CreateOfferAsync(string auth0Id, Guid seasonId, CreateTransferOfferDto dto)
	{
		if (dto.TransferFee <= 0)
			throw new InvalidOperationException("Offer amount must be greater than zero.");

		var me = await GetUserAsync(auth0Id);
		var myClub = await GetMyClubAsync(me.UserId, seasonId)
			?? throw new InvalidOperationException("You do not have a club this season.");

		if (myClub.CurrentBudget < dto.TransferFee)
			throw new InvalidOperationException("Not enough budget for this offer.");

		var rosterPlayer = await _context.UserClubPlayers
			.Include(x => x.UserClub)
			.Include(x => x.GamePlayer)
			.FirstOrDefaultAsync(x =>
				x.GamePlayerId == dto.GamePlayerId &&
				!x.IsDeleted &&
				!x.UserClub.IsDeleted &&
				x.UserClub.SeasonId == seasonId);

		if (rosterPlayer is null)
			throw new InvalidOperationException("That player is not on a club roster this season.");

		if (rosterPlayer.UserClubId == myClub.UserClubId)
			throw new InvalidOperationException("You already own this player.");

		var existing = await _context.TransferRecords.AnyAsync(x =>
			x.SeasonId == seasonId &&
			!x.IsDeleted &&
			x.TransferType == OfferPending &&
			x.GamePlayerId == dto.GamePlayerId &&
			x.ToUserClubId == myClub.UserClubId);

		if (existing)
			throw new InvalidOperationException("You already have a pending offer for this player.");

		var record = new TransferRecord
		{
			TransferRecordId = Guid.NewGuid(),
			SeasonId = seasonId,
			GamePlayerId = dto.GamePlayerId,
			FromUserClubId = rosterPlayer.UserClubId,
			ToUserClubId = myClub.UserClubId,
			PlayerMarketValue = rosterPlayer.GamePlayer.ValueEur,
			TransferFee = dto.TransferFee,
			TransferDateUTC = DateTime.UtcNow,
			TransferType = OfferPending,
			Notes = $"{myClub.Name} offered {dto.TransferFee:N0} for {rosterPlayer.GamePlayer.Name}"
		};

		_context.TransferRecords.Add(record);

		await NotifyAsync(
			rosterPlayer.UserClub.UserId,
			"Transfer offer",
			$"{myClub.Name} offered €{dto.TransferFee:N0} for {rosterPlayer.GamePlayer.Name}.",
			"/transfers");

		await _context.SaveChangesAsync();

		return await MapOffer(record.TransferRecordId, myClub.UserClubId);
	}

	public async Task<TransferOfferDto> AcceptOfferAsync(string auth0Id, Guid transferRecordId)
	{
		var me = await GetUserAsync(auth0Id);
		var offer = await _context.TransferRecords
			.Include(x => x.GamePlayer)
			.Include(x => x.FromUserClub)
			.Include(x => x.ToUserClub)
			.FirstOrDefaultAsync(x => x.TransferRecordId == transferRecordId && !x.IsDeleted)
			?? throw new InvalidOperationException("Offer not found.");

		if (offer.TransferType != OfferPending)
			throw new InvalidOperationException("This offer is no longer pending.");

		if (offer.FromUserClub is null || offer.FromUserClub.UserId != me.UserId)
			throw new InvalidOperationException("Only the selling club can accept this offer.");

		var buyer = offer.ToUserClub;
		if (buyer.CurrentBudget < offer.TransferFee)
			throw new InvalidOperationException("The buying club no longer has enough budget.");

		var rosterPlayer = await _context.UserClubPlayers
			.Include(x => x.LineupPlayers)
			.FirstOrDefaultAsync(x =>
				x.UserClubId == offer.FromUserClubId &&
				x.GamePlayerId == offer.GamePlayerId &&
				!x.IsDeleted)
			?? throw new InvalidOperationException("The player is no longer on the selling club.");

		var alreadyOnBuyer = await _context.UserClubPlayers.AnyAsync(x =>
			x.UserClubId == buyer.UserClubId &&
			x.GamePlayerId == offer.GamePlayerId &&
			!x.IsDeleted);

		if (alreadyOnBuyer)
			throw new InvalidOperationException("The buying club already has this player.");

		if (rosterPlayer.LineupPlayers.Count > 0)
			_context.ClubLineupPlayers.RemoveRange(rosterPlayer.LineupPlayers);

		rosterPlayer.IsDeleted = true;
		rosterPlayer.UpdatedAtUTC = DateTime.UtcNow;

		_context.UserClubPlayers.Add(new UserClubPlayer
		{
			UserClubPlayerId = Guid.NewGuid(),
			UserClubId = buyer.UserClubId,
			GamePlayerId = offer.GamePlayerId,
			AcquisitionPrice = offer.TransferFee,
			AcquiredAtUTC = DateTime.UtcNow,
			AcquisitionType = TransferCompleted
		});

		buyer.CurrentBudget -= offer.TransferFee;
		offer.FromUserClub.CurrentBudget += offer.TransferFee;

		_context.UserClubBudgetTransactions.AddRange(
			new UserClubBudgetTransaction
			{
				UserClubBudgetTransactionId = Guid.NewGuid(),
				UserClubId = buyer.UserClubId,
				SeasonId = offer.SeasonId,
				Amount = -offer.TransferFee,
				Type = "TransferIn",
				Description = $"Bought {offer.GamePlayer.Name}"
			},
			new UserClubBudgetTransaction
			{
				UserClubBudgetTransactionId = Guid.NewGuid(),
				UserClubId = offer.FromUserClub.UserClubId,
				SeasonId = offer.SeasonId,
				Amount = offer.TransferFee,
				Type = "TransferOut",
				Description = $"Sold {offer.GamePlayer.Name}"
			});

		var otherPending = await _context.TransferRecords
			.Where(x =>
				x.SeasonId == offer.SeasonId &&
				!x.IsDeleted &&
				x.TransferType == OfferPending &&
				x.GamePlayerId == offer.GamePlayerId &&
				x.TransferRecordId != offer.TransferRecordId)
			.ToListAsync();

		foreach (var pending in otherPending)
		{
			pending.TransferType = OfferRejected;
			pending.Notes = "Automatically rejected because the player was sold.";
		}

		offer.TransferType = TransferCompleted;
		offer.TransferDateUTC = DateTime.UtcNow;
		offer.Notes = $"{offer.FromUserClub.Name} sold {offer.GamePlayer.Name} to {buyer.Name} for {offer.TransferFee:N0}";

		await NotifyAsync(
			buyer.UserId,
			"Offer accepted",
			$"{offer.FromUserClub.Name} accepted your offer for {offer.GamePlayer.Name}.",
			"/transfers");

		await _context.SaveChangesAsync();
		await _realtime.PublishDraftEventAsync(offer.SeasonId, "transferCompleted");

		return await MapOffer(offer.TransferRecordId, offer.FromUserClub.UserClubId);
	}

	public async Task<TransferOfferDto> RejectOfferAsync(string auth0Id, Guid transferRecordId)
	{
		var me = await GetUserAsync(auth0Id);
		var offer = await LoadPendingAsync(transferRecordId);

		if (offer.FromUserClub is null || offer.FromUserClub.UserId != me.UserId)
			throw new InvalidOperationException("Only the selling club can reject this offer.");

		offer.TransferType = OfferRejected;
		offer.Notes = "Offer rejected.";

		await NotifyAsync(
			offer.ToUserClub.UserId,
			"Offer rejected",
			$"{offer.FromUserClub.Name} rejected the offer for {offer.GamePlayer.Name}.",
			"/transfers");

		await _context.SaveChangesAsync();
		return await MapOffer(offer.TransferRecordId, offer.FromUserClub.UserClubId);
	}

	public async Task CancelOfferAsync(string auth0Id, Guid transferRecordId)
	{
		var me = await GetUserAsync(auth0Id);
		var offer = await LoadPendingAsync(transferRecordId);

		if (offer.ToUserClub.UserId != me.UserId)
			throw new InvalidOperationException("Only the buying club can cancel this offer.");

		offer.TransferType = OfferCancelled;
		offer.Notes = "Offer cancelled.";
		await _context.SaveChangesAsync();
	}

	private async Task<TransferRecord> LoadPendingAsync(Guid transferRecordId)
	{
		var offer = await _context.TransferRecords
			.Include(x => x.GamePlayer)
			.Include(x => x.FromUserClub)
			.Include(x => x.ToUserClub)
			.FirstOrDefaultAsync(x => x.TransferRecordId == transferRecordId && !x.IsDeleted)
			?? throw new InvalidOperationException("Offer not found.");

		if (offer.TransferType != OfferPending)
			throw new InvalidOperationException("This offer is no longer pending.");

		return offer;
	}

	private IQueryable<TransferOfferDto> QueryOffers(Guid seasonId)
	{
		return _context.TransferRecords
			.AsNoTracking()
			.Where(x => x.SeasonId == seasonId && !x.IsDeleted)
			.OrderByDescending(x => x.TransferDateUTC)
			.Select(x => new TransferOfferDto
			{
				TransferRecordId = x.TransferRecordId,
				SeasonId = x.SeasonId,
				GamePlayerId = x.GamePlayerId,
				PlayerName = x.GamePlayer.Name,
				Position = x.GamePlayer.Position,
				Overall = x.GamePlayer.Overall,
				ImageUrl = x.GamePlayer.ImageUrl,
				FromUserClubId = x.FromUserClubId,
				FromClubName = x.FromUserClub != null ? x.FromUserClub.Name : string.Empty,
				ToUserClubId = x.ToUserClubId,
				ToClubName = x.ToUserClub.Name,
				TransferFee = x.TransferFee,
				PlayerMarketValue = x.PlayerMarketValue,
				TransferType = x.TransferType,
				TransferDateUTC = x.TransferDateUTC
			});
	}

	private async Task<TransferOfferDto> MapOffer(Guid id, Guid myClubId)
	{
		var row = await _context.TransferRecords
			.AsNoTracking()
			.Where(x => x.TransferRecordId == id)
			.Select(x => new TransferOfferDto
			{
				TransferRecordId = x.TransferRecordId,
				SeasonId = x.SeasonId,
				GamePlayerId = x.GamePlayerId,
				PlayerName = x.GamePlayer.Name,
				Position = x.GamePlayer.Position,
				Overall = x.GamePlayer.Overall,
				ImageUrl = x.GamePlayer.ImageUrl,
				FromUserClubId = x.FromUserClubId,
				FromClubName = x.FromUserClub != null ? x.FromUserClub.Name : string.Empty,
				ToUserClubId = x.ToUserClubId,
				ToClubName = x.ToUserClub.Name,
				TransferFee = x.TransferFee,
				PlayerMarketValue = x.PlayerMarketValue,
				TransferType = x.TransferType,
				TransferDateUTC = x.TransferDateUTC,
				IsIncoming = x.FromUserClubId == myClubId
			})
			.FirstAsync();

		return row;
	}

	private async Task NotifyAsync(Guid userId, string title, string message, string url)
	{
		var notification = new Notification
		{
			NotificationId = Guid.NewGuid(),
			UserId = userId,
			Title = title,
			Message = message,
			Type = "transfer",
			Url = url,
			IsRead = false
		};

		_context.Notifications.Add(notification);

		await _realtime.NotificationCreatedAsync(userId, new NotificationDto
		{
			NotificationId = notification.NotificationId,
			Title = title,
			Message = message,
			Type = "transfer",
			Url = url,
			IsRead = false,
			CreatedAtUTC = DateTime.UtcNow
		});

		await _push.SendToUserAsync(
			userId,
			title,
			message,
			new Dictionary<string, string>
			{
				{ "type", "transfer" },
				{ "url", url },
				{ "notificationId", notification.NotificationId.ToString() }
			});
	}

	private async Task<backend.Entities.Identity.User> GetUserAsync(string auth0Id)
	{
		return await _context.Users.FirstOrDefaultAsync(x => x.Auth0Id == auth0Id && !x.IsDeleted)
			?? throw new UnauthorizedAccessException("User not found.");
	}

	private Task<UserClub?> GetMyClubAsync(Guid userId, Guid seasonId)
	{
		return _context.UserClubs.FirstOrDefaultAsync(x =>
			x.UserId == userId &&
			x.SeasonId == seasonId &&
			!x.IsDeleted);
	}
}

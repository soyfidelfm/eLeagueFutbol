using backend.Data;
using backend.Entities.Draft;
using backend.Features.Draft.Dtos;
using backend.Features.Draft.Hubs;
using backend.Features.Messages.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.Draft.Services;

public class DraftPickTradeService : IDraftPickTradeService
{
	private readonly AppDbContext _context;
	private readonly IMessageService _messageService;
	private readonly IHubContext<DraftHub> _draftHub;

	public DraftPickTradeService(
		AppDbContext context,
		IMessageService messageService,
		IHubContext<DraftHub> draftHub)
	{
		_context = context;
		_messageService = messageService;
		_draftHub = draftHub;
	}

	public async Task<List<DraftPickAssetDto>> GetMyTradablePicksAsync(
		Guid seasonId,
		string auth0Id)
	{
		var user = await _context.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted &&
				x.IsActive);

		if (user is null)
			throw new Exception("User not found.");

		var userClub = await _context.UserClubs
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.SeasonId == seasonId &&
				x.UserId == user.UserId &&
				!x.IsDeleted);

		if (userClub is null)
			throw new Exception("User club not found for this season.");

		var currentPickNumber = await _context.DraftSessions
			.AsNoTracking()
			.Where(x => x.SeasonId == seasonId && x.IsActive)
			.Select(x => (int?)x.CurrentPickNumber)
			.FirstOrDefaultAsync();

		var query = _context.DraftPickAssets
			.AsNoTracking()
			.Include(x => x.OriginalUserClub)
			.Include(x => x.CurrentOwnerUserClub)
			.Where(x =>
				x.SeasonId == seasonId &&
				x.CurrentOwnerUserClubId == userClub.UserClubId &&
				!x.IsUsed);

		if (currentPickNumber.HasValue)
		{
			query = query.Where(x => x.PickNumber > currentPickNumber.Value);
		}

		return await query
			.OrderBy(x => x.RoundNumber)
			.ThenBy(x => x.PickNumber)
			.Select(x => new DraftPickAssetDto
			{
				DraftPickAssetId = x.DraftPickAssetId,
				SeasonId = x.SeasonId,
				RoundNumber = x.RoundNumber,
				PickNumber = x.PickNumber,
				OriginalUserClubId = x.OriginalUserClubId,
				OriginalClubName = x.OriginalUserClub.Name,
				CurrentOwnerUserClubId = x.CurrentOwnerUserClubId,
				CurrentOwnerClubName = x.CurrentOwnerUserClub.Name,
				IsUsed = x.IsUsed
			})
			.ToListAsync();
	}

	public async Task<DraftPickTradeDto> ProposeTradeAsync(
		Guid seasonId,
		string auth0Id,
		ProposeDraftPickTradeDto dto)
	{
		var user = await _context.Users
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted &&
				x.IsActive);

		if (user is null)
			throw new Exception("User not found.");

		var fromClub = await _context.UserClubs
			.FirstOrDefaultAsync(x =>
				x.SeasonId == seasonId &&
				x.UserId == user.UserId &&
				!x.IsDeleted);

		if (fromClub is null)
			throw new Exception("User club not found for this season.");

		if (dto.ToUserClubId == fromClub.UserClubId)
			throw new Exception("You cannot trade picks with your own club.");

		var toClub = await _context.UserClubs
			.FirstOrDefaultAsync(x =>
				x.SeasonId == seasonId &&
				x.UserClubId == dto.ToUserClubId &&
				!x.IsDeleted);

		if (toClub is null)
			throw new Exception("Target club not found.");

		if (dto.DraftPickAssetIds.Count == 0)
			throw new Exception("No draft picks were selected.");

		var session = await _context.DraftSessions
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.SeasonId == seasonId);

		var pickAssets = await _context.DraftPickAssets
			.Where(x =>
				x.SeasonId == seasonId &&
				dto.DraftPickAssetIds.Contains(x.DraftPickAssetId))
			.ToListAsync();

		if (pickAssets.Count != dto.DraftPickAssetIds.Distinct().Count())
			throw new Exception("One or more draft picks were not found.");

		if (pickAssets.Any(x => x.IsUsed))
			throw new Exception("One or more draft picks have already been used.");

		if (session is not null && session.IsActive)
		{
			if (pickAssets.Any(x => x.PickNumber <= session.CurrentPickNumber))
				throw new Exception("You can only trade future picks.");
		}

		if (pickAssets.Any(x => x.CurrentOwnerUserClubId != fromClub.UserClubId))
			throw new Exception("You can only trade picks that you currently own.");

		var pendingTradeExists = await _context.DraftPickTrades
			.AnyAsync(x =>
				x.SeasonId == seasonId &&
				x.FromUserClubId == fromClub.UserClubId &&
				x.ToUserClubId == toClub.UserClubId &&
				x.Status == "Pending");

		if (pendingTradeExists)
			throw new Exception("There is already a pending trade with this club.");

		await using var transaction = await _context.Database.BeginTransactionAsync();

		var trade = new DraftPickTrade
		{
			DraftPickTradeId = Guid.NewGuid(),
			SeasonId = seasonId,
			FromUserClubId = fromClub.UserClubId,
			ToUserClubId = toClub.UserClubId,
			Status = "Pending",
			Message = dto.Message ?? string.Empty,
			CreatedAtUTC = DateTime.UtcNow,
			RespondedAtUTC = null
		};

		_context.DraftPickTrades.Add(trade);

		foreach (var pickAsset in pickAssets)
		{
			_context.DraftPickTradeAssets.Add(new DraftPickTradeAsset
			{
				DraftPickTradeAssetId = Guid.NewGuid(),
				DraftPickTradeId = trade.DraftPickTradeId,
				DraftPickAssetId = pickAsset.DraftPickAssetId,
				CreatedAtUTC = DateTime.UtcNow
			});
		}

		await _messageService.SendSystemMessageAsync(
			toClub.UserId,
			"Nueva propuesta de trade",
			$"{fromClub.Name} te propuso cambiar picks del Draft.");

		await _context.SaveChangesAsync();
		await transaction.CommitAsync();

		var result = await GetTradeDtoAsync(trade.DraftPickTradeId);

		await _draftHub.Clients
			.Group($"draft-{seasonId}")
			.SendAsync("DraftPickTradeProposed", result);

		return result;
	}

	public async Task<DraftPickTradeDto> AcceptTradeAsync(
		Guid seasonId,
		string auth0Id,
		Guid tradeId)
	{
		var user = await _context.Users
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted &&
				x.IsActive);

		if (user is null)
			throw new Exception("User not found.");

		var myClub = await _context.UserClubs
			.FirstOrDefaultAsync(x =>
				x.SeasonId == seasonId &&
				x.UserId == user.UserId &&
				!x.IsDeleted);

		if (myClub is null)
			throw new Exception("User club not found for this season.");

		var trade = await _context.DraftPickTrades
			.Include(x => x.TradeAssets)
			.FirstOrDefaultAsync(x =>
				x.SeasonId == seasonId &&
				x.DraftPickTradeId == tradeId);

		if (trade is null)
			throw new Exception("Trade not found.");

		if (trade.ToUserClubId != myClub.UserClubId)
			throw new Exception("Only the receiving club can accept this trade.");

		if (trade.Status != "Pending")
			throw new Exception("This trade is no longer pending.");

		var session = await _context.DraftSessions
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.SeasonId == seasonId);

		var pickAssetIds = trade.TradeAssets
			.Select(x => x.DraftPickAssetId)
			.ToList();

		var pickAssets = await _context.DraftPickAssets
			.Where(x =>
				x.SeasonId == seasonId &&
				pickAssetIds.Contains(x.DraftPickAssetId))
			.ToListAsync();

		if (pickAssets.Any(x => x.IsUsed))
			throw new Exception("One or more picks have already been used.");

		if (session is not null && session.IsActive)
		{
			if (pickAssets.Any(x => x.PickNumber <= session.CurrentPickNumber))
				throw new Exception("One or more picks are no longer tradable.");
		}

		if (pickAssets.Any(x => x.CurrentOwnerUserClubId != trade.FromUserClubId))
			throw new Exception("One or more picks are no longer owned by the proposing club.");

		await using var transaction = await _context.Database.BeginTransactionAsync();

		foreach (var pickAsset in pickAssets)
		{
			pickAsset.CurrentOwnerUserClubId = trade.ToUserClubId;
			pickAsset.UpdatedAtUTC = DateTime.UtcNow;
		}

		trade.Status = "Accepted";
		trade.RespondedAtUTC = DateTime.UtcNow;

		var fromUserId = await _context.UserClubs
			.AsNoTracking()
			.Where(x => x.UserClubId == trade.FromUserClubId)
			.Select(x => x.UserId)
			.FirstAsync();

		await _messageService.SendSystemMessageAsync(
			fromUserId,
			"Trade aceptado",
			"Tu propuesta de trade de picks fue aceptada.");

		await _context.SaveChangesAsync();
		await transaction.CommitAsync();

		var result = await GetTradeDtoAsync(trade.DraftPickTradeId);

		await _draftHub.Clients
			.Group($"draft-{seasonId}")
			.SendAsync("DraftPickTradeAccepted", result);

		return result;
	}

	public async Task RejectTradeAsync(
		Guid seasonId,
		string auth0Id,
		Guid tradeId)
	{
		var user = await _context.Users
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted &&
				x.IsActive);

		if (user is null)
			throw new Exception("User not found.");

		var myClub = await _context.UserClubs
			.FirstOrDefaultAsync(x =>
				x.SeasonId == seasonId &&
				x.UserId == user.UserId &&
				!x.IsDeleted);

		if (myClub is null)
			throw new Exception("User club not found for this season.");

		var trade = await _context.DraftPickTrades
			.FirstOrDefaultAsync(x =>
				x.SeasonId == seasonId &&
				x.DraftPickTradeId == tradeId);

		if (trade is null)
			throw new Exception("Trade not found.");

		if (trade.ToUserClubId != myClub.UserClubId)
			throw new Exception("Only the receiving club can reject this trade.");

		if (trade.Status != "Pending")
			throw new Exception("This trade is no longer pending.");

		trade.Status = "Rejected";
		trade.RespondedAtUTC = DateTime.UtcNow;

		var fromUserId = await _context.UserClubs
			.AsNoTracking()
			.Where(x => x.UserClubId == trade.FromUserClubId)
			.Select(x => x.UserId)
			.FirstAsync();

		await _messageService.SendSystemMessageAsync(
			fromUserId,
			"Trade rechazado",
			"Tu propuesta de trade de picks fue rechazada.");

		await _context.SaveChangesAsync();

		var result = await GetTradeDtoAsync(trade.DraftPickTradeId);

		await _draftHub.Clients
			.Group($"draft-{seasonId}")
			.SendAsync("DraftPickTradeRejected", result);
	}

	public async Task CancelTradeAsync(
		Guid seasonId,
		string auth0Id,
		Guid tradeId)
	{
		var user = await _context.Users
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted &&
				x.IsActive);

		if (user is null)
			throw new Exception("User not found.");

		var myClub = await _context.UserClubs
			.FirstOrDefaultAsync(x =>
				x.SeasonId == seasonId &&
				x.UserId == user.UserId &&
				!x.IsDeleted);

		if (myClub is null)
			throw new Exception("User club not found for this season.");

		var trade = await _context.DraftPickTrades
			.FirstOrDefaultAsync(x =>
				x.SeasonId == seasonId &&
				x.DraftPickTradeId == tradeId);

		if (trade is null)
			throw new Exception("Trade not found.");

		if (trade.FromUserClubId != myClub.UserClubId)
			throw new Exception("Only the proposing club can cancel this trade.");

		if (trade.Status != "Pending")
			throw new Exception("This trade is no longer pending.");

		trade.Status = "Cancelled";
		trade.RespondedAtUTC = DateTime.UtcNow;

		var toUserId = await _context.UserClubs
			.AsNoTracking()
			.Where(x => x.UserClubId == trade.ToUserClubId)
			.Select(x => x.UserId)
			.FirstAsync();

		await _messageService.SendSystemMessageAsync(
			toUserId,
			"Trade cancelado",
			"Una propuesta de trade de picks fue cancelada.");

		await _context.SaveChangesAsync();

		var result = await GetTradeDtoAsync(trade.DraftPickTradeId);

		await _draftHub.Clients
			.Group($"draft-{seasonId}")
			.SendAsync("DraftPickTradeCancelled", result);
	}

	private async Task<DraftPickTradeDto> GetTradeDtoAsync(Guid tradeId)
	{
		var trade = await _context.DraftPickTrades
			.AsNoTracking()
			.Include(x => x.FromUserClub)
			.Include(x => x.ToUserClub)
			.Include(x => x.TradeAssets)
				.ThenInclude(x => x.DraftPickAsset)
					.ThenInclude(x => x.OriginalUserClub)
			.Include(x => x.TradeAssets)
				.ThenInclude(x => x.DraftPickAsset)
					.ThenInclude(x => x.CurrentOwnerUserClub)
			.FirstAsync(x => x.DraftPickTradeId == tradeId);

		return new DraftPickTradeDto
		{
			DraftPickTradeId = trade.DraftPickTradeId,
			SeasonId = trade.SeasonId,
			FromUserClubId = trade.FromUserClubId,
			FromClubName = trade.FromUserClub.Name,
			ToUserClubId = trade.ToUserClubId,
			ToClubName = trade.ToUserClub.Name,
			Status = trade.Status,
			Message = trade.Message,
			CreatedAtUTC = trade.CreatedAtUTC,
			RespondedAtUTC = trade.RespondedAtUTC,
			Picks = trade.TradeAssets
				.OrderBy(x => x.DraftPickAsset.RoundNumber)
				.ThenBy(x => x.DraftPickAsset.PickNumber)
				.Select(x => new DraftPickAssetDto
				{
					DraftPickAssetId = x.DraftPickAsset.DraftPickAssetId,
					SeasonId = x.DraftPickAsset.SeasonId,
					RoundNumber = x.DraftPickAsset.RoundNumber,
					PickNumber = x.DraftPickAsset.PickNumber,
					OriginalUserClubId = x.DraftPickAsset.OriginalUserClubId,
					OriginalClubName = x.DraftPickAsset.OriginalUserClub.Name,
					CurrentOwnerUserClubId = x.DraftPickAsset.CurrentOwnerUserClubId,
					CurrentOwnerClubName = x.DraftPickAsset.CurrentOwnerUserClub.Name,
					IsUsed = x.DraftPickAsset.IsUsed
				})
				.ToList()
		};
	}
}
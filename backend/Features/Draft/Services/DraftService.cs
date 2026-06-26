using backend.Data;
using backend.Entities.Clubs;
using backend.Entities.Draft;
using backend.Entities.Transfers;
using backend.Features.Draft.Dtos;
using backend.Features.Draft.Hubs;
using backend.Features.Messages.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.Draft.Services;

public class DraftService : IDraftService
{
	private readonly AppDbContext _context;
	private readonly IMessageService _messageService;
	private readonly IDraftTurnService _draftTurnService;
	private readonly IHubContext<DraftHub> _draftHub;

	public DraftService(
		AppDbContext context,
		IMessageService messageService,
		IDraftTurnService draftTurnService,
		IHubContext<DraftHub> draftHub)
	{
		_context = context;
		_messageService = messageService;
		_draftTurnService = draftTurnService;
		_draftHub = draftHub;
	}

	public async Task<DraftLotteryPreviewDto> PreviewDraftOrderAsync(
		Guid seasonId,
		int drawNumber,
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

		if (!string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
			throw new Exception("Only admins can preview draft order.");

		if (drawNumber < 1 || drawNumber > 3)
			throw new Exception("Draw number must be between 1 and 3.");

		var draftAlreadyExists = await _context.DraftSessions
			.AnyAsync(x => x.SeasonId == seasonId);

		if (draftAlreadyExists)
			throw new Exception("Draft already exists for this season.");

		var registrations = await _context.SeasonRegistrations
			.AsNoTracking()
			.Include(x => x.User)
			.Where(x =>
				x.SeasonId == seasonId &&
				!x.User.IsDeleted &&
				x.User.IsActive)
			.ToListAsync();

		if (registrations.Count == 0)
			throw new Exception("No registered users found for this season.");

		var userClubs = await _context.UserClubs
			.AsNoTracking()
			.Where(x =>
				x.SeasonId == seasonId &&
				!x.IsDeleted)
			.ToDictionaryAsync(x => x.UserId);

		var shuffled = registrations
			.OrderBy(x => Guid.NewGuid())
			.Select((registration, index) =>
			{
				userClubs.TryGetValue(registration.UserId, out var club);

				return new DraftOrderDto
				{
					PickOrder = index + 1,
					UserId = registration.UserId,
					ClubName = club?.Name ?? registration.User.DisplayName,
					LogoUrl = club?.LogoUrl ?? string.Empty,
					IsCurrentPick = index == 0
				};
			})
			.ToList();

		await SendDraftLotteryMessagesAsync(seasonId, drawNumber, shuffled);

		return new DraftLotteryPreviewDto
		{
			DrawNumber = drawNumber,
			IsFinal = drawNumber == 3,
			DraftOrder = shuffled
		};
	}

	public async Task<DraftCenterDto?> GetCenterAsync(Guid seasonId, string auth0Id)
	{
		var user = await _context.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Auth0Id == auth0Id && !x.IsDeleted);

		if (user is null)
			return null;

		var session = await _context.DraftSessions
			.FirstOrDefaultAsync(x => x.SeasonId == seasonId);

		var currentBudget = await _context.UserClubs
			.AsNoTracking()
			.Where(x => x.SeasonId == seasonId && x.UserId == user.UserId)
			.Select(x => x.CurrentBudget)
			.FirstOrDefaultAsync();

		if (session is null)
		{
			return new DraftCenterDto
			{
				MyUserId = user.UserId,
				CurrentBudget = currentBudget,
				State = new DraftStateDto
				{
					DraftSessionId = Guid.Empty,
					IsActive = false,
					CurrentRound = 0,
					CurrentPickNumber = 0,
					CurrentUserId = null,
					CurrentClubName = "Draft not started",
					SecondsRemaining = 0,
					TotalTeams = 0,
					PlayersPerTeam = 25
				},
				DraftOrder = [],
				RecentPicks = [],
				MySquad = new MyDraftSquadDto
				{
					TotalPlayers = 0,
					Players = []
				}
			};
		}

		await _draftTurnService.ProcessDraftTimerAsync(session);

		return await _draftTurnService.BuildDraftCenterAsync(
			seasonId,
			user.UserId,
			session);
	}

	public async Task<DraftCenterDto> StartDraftAsync(
		Guid seasonId,
		string auth0Id)
	{
		var user = await _context.Users
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted &&
				x.IsActive);

		if (user is null)
			throw new Exception("User not found.");

		if (!string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
			throw new Exception("Only admins can start the draft.");

		await using var transaction = await _context.Database.BeginTransactionAsync();

		var session = await _context.DraftSessions
			.FirstOrDefaultAsync(x => x.SeasonId == seasonId);

		if (session is null)
			throw new Exception("Draft order has not been generated.");

		if (session.IsActive)
			throw new Exception("Draft is already active.");

		var order = await _draftTurnService.GetDraftOrderAsync(seasonId);

		if (order.Count == 0)
			throw new Exception("Draft order has not been generated.");

		var firstPickAssetExists = await _context.DraftPickAssets
			.AnyAsync(x =>
				x.SeasonId == seasonId &&
				x.PickNumber == 1 &&
				!x.IsUsed);

		if (!firstPickAssetExists)
			throw new Exception("Draft pick assets have not been generated.");

		var now = DateTime.UtcNow;

		session.IsActive = true;
		session.CurrentPickNumber = 1;
		session.CurrentPickStartedAtUTC = now;
		session.OneMinuteWarningSent = false;

		var currentClub = await _draftTurnService.GetCurrentPickOwnerClubAsync(session);

		if (currentClub is null)
			throw new Exception("First draft turn not found.");

		await SendSystemAsync(
			currentClub.UserId,
			"It's Your Turn to Draft",
			"You are now on the clock. You have 10 minutes to make your selection.");

		await _draftHub.Clients
			.Group($"draft-{seasonId}")
			.SendAsync("DraftTurnStarted", new
			{
				seasonId,
				pickNumber = session.CurrentPickNumber,
				roundNumber = 1,
				clubName = currentClub.Name,
				logoUrl = currentClub.LogoUrl,
				userId = currentClub.UserId,
				userClubId = currentClub.UserClubId
			});

		await _context.SaveChangesAsync();
		await transaction.CommitAsync();

		return await _draftTurnService.BuildDraftCenterAsync(
			seasonId,
			user.UserId,
			session);
	}

	public async Task<DraftCenterDto> GenerateDraftOrderAsync(
		Guid seasonId,
		string auth0Id,
		StartDraftDto dto)
	{
		var user = await _context.Users
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted &&
				x.IsActive);

		if (user is null)
			throw new Exception("User not found.");

		if (!string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
			throw new Exception("Only admins can generate the draft order.");

		if (dto.UserIds.Count == 0)
			throw new Exception("Draft order was not provided.");

		var existingSession = await _context.DraftSessions
			.AnyAsync(x => x.SeasonId == seasonId);

		if (existingSession)
			throw new Exception("Draft already exists for this season.");

		await using var transaction = await _context.Database.BeginTransactionAsync();

		var registrations = await _context.SeasonRegistrations
			.AsNoTracking()
			.Where(x => x.SeasonId == seasonId)
			.Select(x => x.UserId)
			.ToListAsync();

		if (dto.UserIds.Any(x => !registrations.Contains(x)))
			throw new Exception("Draft order contains invalid users.");

		if (dto.UserIds.Distinct().Count() != dto.UserIds.Count)
			throw new Exception("Draft order contains duplicated users.");

		if (dto.UserIds.Count != registrations.Count)
			throw new Exception("Draft order does not match registered users.");

		var userClubs = await _context.UserClubs
			.AsNoTracking()
			.Where(x =>
				x.SeasonId == seasonId &&
				!x.IsDeleted &&
				dto.UserIds.Contains(x.UserId))
			.ToDictionaryAsync(x => x.UserId);

		if (userClubs.Count != dto.UserIds.Count)
			throw new Exception("One or more users do not have a club for this season.");

		var now = DateTime.UtcNow;

		var session = new DraftSession
		{
			DraftSessionId = Guid.NewGuid(),
			SeasonId = seasonId,
			IsActive = false,
			CurrentPickNumber = 1,
			PlayersPerTeam = 25,
			CurrentPickStartedAtUTC = null,
			OneMinuteWarningSent = false,
			CreatedAtUTC = now
		};

		_context.DraftSessions.Add(session);

		var draftOrders = dto.UserIds
			.Select((userId, index) => new DraftOrder
			{
				DraftOrderId = Guid.NewGuid(),
				SeasonId = seasonId,
				UserId = userId,
				PickOrder = index + 1,
				CreatedAtUTC = now
			})
			.ToList();

		_context.DraftOrders.AddRange(draftOrders);

		var draftPickAssets = new List<DraftPickAsset>();

		for (var round = 1; round <= session.PlayersPerTeam; round++)
		{
			var isOddRound = round % 2 == 1;

			var roundOrder = isOddRound
				? draftOrders.OrderBy(x => x.PickOrder).ToList()
				: draftOrders.OrderByDescending(x => x.PickOrder).ToList();

			foreach (var draftOrder in roundOrder)
			{
				if (!userClubs.TryGetValue(draftOrder.UserId, out var userClub))
					throw new Exception("User club not found while creating draft pick assets.");

				var pickNumber = draftPickAssets.Count + 1;

				draftPickAssets.Add(new DraftPickAsset
				{
					DraftPickAssetId = Guid.NewGuid(),
					SeasonId = seasonId,
					RoundNumber = round,
					PickNumber = pickNumber,
					OriginalUserClubId = userClub.UserClubId,
					CurrentOwnerUserClubId = userClub.UserClubId,
					IsUsed = false,
					CreatedAtUTC = now
				});
			}
		}

		_context.DraftPickAssets.AddRange(draftPickAssets);

		await _context.SaveChangesAsync();
		await transaction.CommitAsync();

		return await _draftTurnService.BuildDraftCenterAsync(
			seasonId,
			user.UserId,
			session);
	}

	public async Task<List<DraftPlayerDto>> GetAvailablePlayersAsync(
		Guid seasonId,
		string? search,
		string? position,
		int page,
		int pageSize)
	{
		var session = await _context.DraftSessions
			.FirstOrDefaultAsync(x => x.SeasonId == seasonId && x.IsActive);

		if (session is not null)
			await _draftTurnService.ProcessDraftTimerAsync(session);

		page = page <= 0 ? 1 : page;
		pageSize = pageSize <= 0 ? 30 : pageSize;

		var draftedPlayerIds = await _context.DraftPicks
			.AsNoTracking()
			.Where(x => x.SeasonId == seasonId)
			.Select(x => x.GamePlayerId)
			.ToListAsync();

		var query = _context.GamePlayers
			.AsNoTracking()
			.Include(x => x.GameClub)
			.Where(x =>
				!draftedPlayerIds.Contains(x.GamePlayerId) &&
				!x.IsDeleted &&
				x.IsActive);

		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = search.Trim().ToLower();

			query = query.Where(x =>
				x.Name.ToLower().Contains(term) ||
				x.FullName.ToLower().Contains(term));
		}

		if (!string.IsNullOrWhiteSpace(position) && position != "All")
		{
			var pos = position.Trim().ToLower();

			query = query.Where(x =>
				x.Position.ToLower().Contains(pos));
		}

		return await query
			.OrderByDescending(x => x.Overall)
			.ThenBy(x => x.Name)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(x => new DraftPlayerDto
			{
				GamePlayerId = x.GamePlayerId,
				Name = string.IsNullOrWhiteSpace(x.Name) ? x.FullName : x.Name,
				Position = x.Position,
				Overall = x.Overall,
				Potential = x.Potential,
				Pace = x.Pace ?? 0,
				Shooting = x.Shooting ?? 0,
				Passing = x.Passing ?? 0,
				Dribbling = x.Dribbling ?? 0,
				Defending = x.Defending ?? 0,
				Physic = x.Physic ?? 0,
				Value = x.ValueEur ?? 0,
				Nation = x.Nation,
				Age = x.Age,
				ImageUrl = x.ImageUrl,
				GameClubName = x.GameClub.Name
			})
			.ToListAsync();
	}

	public async Task<List<DraftPickDto>> GetPicksAsync(Guid seasonId)
	{
		var userClubs = await _context.UserClubs
			.AsNoTracking()
			.Where(x => x.SeasonId == seasonId && !x.IsDeleted)
			.ToDictionaryAsync(x => x.UserId);

		var picks = await _context.DraftPicks
			.AsNoTracking()
			.Include(x => x.User)
			.Include(x => x.GamePlayer)
			.Where(x => x.SeasonId == seasonId)
			.OrderBy(x => x.PickNumber)
			.ToListAsync();

		return picks.Select(x => new DraftPickDto
		{
			PickNumber = x.PickNumber,
			RoundNumber = x.RoundNumber,
			UserId = x.UserId,
			ClubName = userClubs.TryGetValue(x.UserId, out var club)
				? club.Name
				: x.User.DisplayName,
			GamePlayerId = x.GamePlayerId,
			PlayerName = string.IsNullOrWhiteSpace(x.GamePlayer.Name)
				? x.GamePlayer.FullName
				: x.GamePlayer.Name,
			Position = x.GamePlayer.Position,
			Overall = x.GamePlayer.Overall,
			PlayerImageUrl = x.GamePlayer.ImageUrl,
			PickedAtUTC = x.PickedAtUTC
		}).ToList();
	}

	public async Task<DraftPickDto> MakePickAsync(
		Guid seasonId,
		string auth0Id,
		MakeDraftPickDto dto)
	{
		await using var transaction = await _context.Database.BeginTransactionAsync();

		var user = await _context.Users
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted &&
				x.IsActive);

		if (user is null)
			throw new Exception("User not found.");

		var userClub = await _context.UserClubs
			.FirstOrDefaultAsync(x =>
				x.UserId == user.UserId &&
				x.SeasonId == seasonId &&
				!x.IsDeleted);

		if (userClub is null)
			throw new Exception("User club not found for this season.");

		var session = await _context.DraftSessions
			.FirstOrDefaultAsync(x =>
				x.SeasonId == seasonId &&
				x.IsActive);

		if (session is null)
			throw new Exception("Draft is not active.");

		await _draftTurnService.ProcessDraftTimerAsync(session);

		if (!session.IsActive)
			throw new Exception("Draft is not active.");

		var order = await _draftTurnService.GetDraftOrderAsync(seasonId);

		if (order.Count == 0)
			throw new Exception("Draft order has not been generated.");

		var currentPickAsset = await _context.DraftPickAssets
			.FirstOrDefaultAsync(x =>
				x.SeasonId == seasonId &&
				x.PickNumber == session.CurrentPickNumber &&
				!x.IsUsed);

		if (currentPickAsset is null)
			throw new Exception("Current draft pick asset not found.");

		if (currentPickAsset.CurrentOwnerUserClubId != userClub.UserClubId)
			throw new Exception("It is not your turn.");

		var currentSquadCount = await _context.UserClubPlayers
			.CountAsync(x =>
				x.UserClubId == userClub.UserClubId &&
				!x.IsDeleted);

		if (currentSquadCount >= session.PlayersPerTeam)
			throw new Exception("Your squad is already full.");

		var alreadyPicked = await _context.DraftPicks
			.AnyAsync(x =>
				x.SeasonId == seasonId &&
				x.GamePlayerId == dto.GamePlayerId);

		if (alreadyPicked)
			throw new Exception("This player has already been drafted.");

		var player = await _context.GamePlayers
			.FirstOrDefaultAsync(x =>
				x.GamePlayerId == dto.GamePlayerId &&
				!x.IsDeleted &&
				x.IsActive);

		if (player is null)
			throw new Exception("Player not found.");

		var playerName = string.IsNullOrWhiteSpace(player.Name)
			? player.FullName
			: player.Name;

		var price = _draftTurnService.GetDraftPrice(
			player.ValueEur,
			player.Overall);

		if (userClub.CurrentBudget < price)
			throw new Exception("Not enough budget to draft this player.");

		var pick = new DraftPick
		{
			DraftPickId = Guid.NewGuid(),
			SeasonId = seasonId,
			UserId = user.UserId,
			GamePlayerId = dto.GamePlayerId,
			PickNumber = session.CurrentPickNumber,
			RoundNumber = currentPickAsset.RoundNumber,
			PickedAtUTC = DateTime.UtcNow
		};

		_context.DraftPicks.Add(pick);

		_context.UserClubPlayers.Add(new UserClubPlayer
		{
			UserClubPlayerId = Guid.NewGuid(),
			UserClubId = userClub.UserClubId,
			GamePlayerId = player.GamePlayerId,
			AcquisitionPrice = price,
			AcquiredAtUTC = DateTime.UtcNow,
			AcquisitionType = "Draft"
		});

		_context.TransferRecords.Add(new TransferRecord
		{
			TransferRecordId = Guid.NewGuid(),
			SeasonId = seasonId,
			GamePlayerId = player.GamePlayerId,
			FromUserClubId = null,
			ToUserClubId = userClub.UserClubId,
			PlayerMarketValue = player.ValueEur,
			TransferFee = price,
			TransferDateUTC = DateTime.UtcNow,
			TransferType = "Draft",
			Notes = $"Draft pick #{session.CurrentPickNumber}"
		});

		_context.UserClubBudgetTransactions.Add(new UserClubBudgetTransaction
		{
			UserClubBudgetTransactionId = Guid.NewGuid(),
			UserClubId = userClub.UserClubId,
			SeasonId = seasonId,
			Amount = -price,
			Type = "DraftPurchase",
			Description = $"Drafted {playerName} for {price:N0}"
		});

		userClub.CurrentBudget -= price;

		currentPickAsset.IsUsed = true;
		currentPickAsset.UpdatedAtUTC = DateTime.UtcNow;

		await SendSystemAsync(
			user.UserId,
			"Pick Selected",
			$"You selected {playerName} in the Draft.");

		await _draftHub.Clients
			.Group($"draft-{seasonId}")
			.SendAsync("DraftPickMade", new
			{
				seasonId,
				pickNumber = pick.PickNumber,
				roundNumber = pick.RoundNumber,
				clubName = userClub.Name,
				logoUrl = userClub.LogoUrl,
				userId = user.UserId,
				userClubId = userClub.UserClubId,
				playerName,
				playerImageUrl = player.ImageUrl,
				overall = player.Overall,
				position = player.Position
			});

		await _draftTurnService.AdvanceDraftTurnAsync(session, order);

		await _context.SaveChangesAsync();
		await transaction.CommitAsync();

		var result = await GetPicksAsync(seasonId);

		return result.First(x => x.PickNumber == pick.PickNumber);
	}

	private async Task SendSystemAsync(Guid userId, string title, string body)
	{
		await _messageService.SendSystemMessageAsync(userId, title, body);
	}

	private async Task SendDraftLotteryMessagesAsync(
		Guid seasonId,
		int drawNumber,
		List<DraftOrderDto> order)
	{
		var registeredUserIds = await _context.SeasonRegistrations
			.AsNoTracking()
			.Where(x => x.SeasonId == seasonId)
			.Select(x => x.UserId)
			.ToListAsync();

		if (registeredUserIds.Count == 0)
			return;

		var fullOrder = order
			.OrderBy(x => x.PickOrder)
			.Select(x => $"{x.PickOrder}. {x.ClubName}")
			.ToList();

		var orderText = string.Join(Environment.NewLine, fullOrder);

		var title = drawNumber < 3
			? $"Draft Lottery Draw {drawNumber}/3 Completed"
			: "Final Draft Lottery Completed";

		var body = drawNumber < 3
			? $"Draw {drawNumber} finished. Current draft order:{Environment.NewLine}{Environment.NewLine}{orderText}"
			: $"Final draft order is ready:{Environment.NewLine}{Environment.NewLine}{orderText}";

		foreach (var userId in registeredUserIds)
		{
			await SendSystemAsync(userId, title, body);
		}
	}
}
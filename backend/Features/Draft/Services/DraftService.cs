using backend.Data;
using backend.Entities.Clubs;
using backend.Entities.Draft;
using backend.Entities.Transfers;
using backend.Features.Draft.Dtos;
using backend.Features.Messages.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.Draft.Services;

public class DraftService : IDraftService
{
	private readonly AppDbContext _context;
	private readonly IMessageService _messageService;

	private const int PickSeconds = 600; // 10 minutes

	public DraftService(AppDbContext context, IMessageService messageService)
	{
		_context = context;
		_messageService = messageService;
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

		await ProcessDraftTimerAsync(session);

		return await BuildDraftCenterAsync(seasonId, user.UserId, session);
	}

	public async Task<DraftCenterDto> StartDraftAsync(
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
			throw new Exception("Only admins can start the draft.");

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

		var now = DateTime.UtcNow;

		var session = new DraftSession
		{
			DraftSessionId = Guid.NewGuid(),
			SeasonId = seasonId,
			IsActive = true,
			CurrentPickNumber = 1,
			PlayersPerTeam = 25,
			CurrentPickStartedAtUTC = now,
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
				PickOrder = index + 1
			})
			.ToList();

		_context.DraftOrders.AddRange(draftOrders);

		await _context.SaveChangesAsync();

		await SendSystemAsync(
			dto.UserIds.First(),
			"Es tu turno en el Draft",
			"Estás en turno. Tienes 10 minutos para seleccionar tu jugador.");

		await transaction.CommitAsync();

		return await BuildDraftCenterAsync(seasonId, user.UserId, session);
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
			await ProcessDraftTimerAsync(session);

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
			query = query.Where(x => x.Position.ToLower().Contains(pos));
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

		await ProcessDraftTimerAsync(session);

		if (!session.IsActive)
			throw new Exception("Draft is not active.");

		var order = await GetDraftOrderAsync(seasonId);

		if (order.Count == 0)
			throw new Exception("Draft order has not been generated.");

		var currentTurn = GetCurrentTurn(session, order);

		if (currentTurn is null)
			throw new Exception("Current draft turn not found.");

		if (currentTurn.UserId != user.UserId)
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

		var price = GetDraftPrice(player.ValueEur, player.Overall);

		if (userClub.CurrentBudget < price)
			throw new Exception("Not enough budget to draft this player.");

		var roundNumber = ((session.CurrentPickNumber - 1) / order.Count) + 1;

		var pick = new DraftPick
		{
			DraftPickId = Guid.NewGuid(),
			SeasonId = seasonId,
			UserId = user.UserId,
			GamePlayerId = dto.GamePlayerId,
			PickNumber = session.CurrentPickNumber,
			RoundNumber = roundNumber,
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
			Description = $"Drafted {player.Name} for {price:N0}"
		});

		userClub.CurrentBudget -= price;

		await SendSystemAsync(
			user.UserId,
			"Pick seleccionado",
			$"Seleccionaste a {player.Name} en el Draft.");

		await AdvanceDraftTurnAsync(session, order);

		await _context.SaveChangesAsync();
		await transaction.CommitAsync();

		var result = await GetPicksAsync(seasonId);

		return result.First(x => x.PickNumber == pick.PickNumber);
	}

	private async Task<DraftCenterDto> BuildDraftCenterAsync(
		Guid seasonId,
		Guid userId,
		DraftSession session)
	{
		var userClubs = await _context.UserClubs
			.AsNoTracking()
			.Include(x => x.BaseGameClub)
			.Where(x => x.SeasonId == seasonId && !x.IsDeleted)
			.ToDictionaryAsync(x => x.UserId);

		var myClub = userClubs.TryGetValue(userId, out var foundClub)
			? foundClub
			: null;

		var order = await _context.DraftOrders
			.AsNoTracking()
			.Include(x => x.User)
			.Where(x => x.SeasonId == seasonId)
			.OrderBy(x => x.PickOrder)
			.Select(x => new DraftOrderDto
			{
				PickOrder = x.PickOrder,
				UserId = x.UserId,
				ClubName = x.User.DisplayName,
				LogoUrl = null,
				IsCurrentPick = false
			})
			.ToListAsync();

		foreach (var item in order)
		{
			if (userClubs.TryGetValue(item.UserId, out var club))
			{
				item.ClubName = club.Name;
				item.LogoUrl = club.LogoUrl ?? string.Empty;
			}
		}

		var picks = await GetPicksAsync(seasonId);

		var totalTeams = order.Count;

		var currentRound = totalTeams == 0
			? 0
			: ((session.CurrentPickNumber - 1) / totalTeams) + 1;

		var currentOrderPosition = totalTeams == 0
			? 0
			: ((session.CurrentPickNumber - 1) % totalTeams) + 1;

		var currentTeam = order.FirstOrDefault(x => x.PickOrder == currentOrderPosition);

		foreach (var item in order)
			item.IsCurrentPick = item.PickOrder == currentOrderPosition;

		var mySquadPlayers = picks
			.Where(x => x.UserId == userId)
			.Select(x => new DraftSquadPlayerDto
			{
				GamePlayerId = x.GamePlayerId,
				Name = x.PlayerName,
				Position = x.Position,
				Overall = x.Overall,
				ImageUrl = x.PlayerImageUrl,
				PickNumber = x.PickNumber
			})
			.ToList();

		return new DraftCenterDto
		{
			MyUserId = userId,
			CurrentBudget = myClub?.CurrentBudget ?? 0,
			State = new DraftStateDto
			{
				DraftSessionId = session.DraftSessionId,
				IsActive = session.IsActive,
				CurrentRound = session.IsActive ? currentRound : 0,
				CurrentPickNumber = session.IsActive ? session.CurrentPickNumber : 0,
				CurrentUserId = session.IsActive ? currentTeam?.UserId : null,
				CurrentClubName = session.IsActive ? currentTeam?.ClubName ?? string.Empty : "Draft finished",
				SecondsRemaining = GetSecondsRemaining(session),
				TotalTeams = totalTeams,
				PlayersPerTeam = session.PlayersPerTeam
			},
			DraftOrder = order,
			RecentPicks = picks.OrderByDescending(x => x.PickNumber).Take(10).ToList(),
			MySquad = new MyDraftSquadDto
			{
				TotalPlayers = mySquadPlayers.Count,
				Players = mySquadPlayers
			}
		};
	}

	private async Task ProcessDraftTimerAsync(DraftSession session)
	{
		if (!session.IsActive || session.CurrentPickStartedAtUTC is null)
			return;

		var order = await GetDraftOrderAsync(session.SeasonId);

		if (order.Count == 0)
			return;

		var currentTurn = GetCurrentTurn(session, order);

		if (currentTurn is null)
			return;

		var remaining = GetSecondsRemaining(session);

		if (remaining <= 60 && !session.OneMinuteWarningSent && remaining > 0)
		{
			session.OneMinuteWarningSent = true;

			await SendSystemAsync(
				currentTurn.UserId,
				"Te queda 1 minuto",
				"Te queda 1 minuto para seleccionar tu pick del Draft.");
		}

		if (remaining <= 0)
		{
			await SendSystemAsync(
				currentTurn.UserId,
				"Tu turno terminó",
				"Se acabó tu tiempo para seleccionar. Tu turno fue saltado.");

			await AdvanceDraftTurnAsync(session, order);
		}

		await _context.SaveChangesAsync();
	}

	private async Task AdvanceDraftTurnAsync(DraftSession session, List<DraftOrder> order)
	{
		var totalPicks = order.Count * session.PlayersPerTeam;

		if (session.CurrentPickNumber >= totalPicks)
		{
			session.IsActive = false;
			session.CurrentPickStartedAtUTC = null;
			session.OneMinuteWarningSent = false;
			return;
		}

		session.CurrentPickNumber++;
		session.CurrentPickStartedAtUTC = DateTime.UtcNow;
		session.OneMinuteWarningSent = false;

		var nextTurn = GetCurrentTurn(session, order);

		if (nextTurn is not null)
		{
			await SendSystemAsync(
				nextTurn.UserId,
				"Es tu turno en el Draft",
				"Estás en turno. Tienes 10 minutos para seleccionar tu jugador.");
		}
	}

	private async Task<List<DraftOrder>> GetDraftOrderAsync(Guid seasonId)
	{
		return await _context.DraftOrders
			.AsNoTracking()
			.Where(x => x.SeasonId == seasonId)
			.OrderBy(x => x.PickOrder)
			.ToListAsync();
	}

	private static DraftOrder? GetCurrentTurn(DraftSession session, List<DraftOrder> order)
	{
		if (order.Count == 0)
			return null;

		var currentOrderPosition = ((session.CurrentPickNumber - 1) % order.Count) + 1;

		return order.FirstOrDefault(x => x.PickOrder == currentOrderPosition);
	}

	private static int GetSecondsRemaining(DraftSession session)
	{
		if (!session.IsActive || session.CurrentPickStartedAtUTC is null)
			return 0;

		var elapsedSeconds = (int)(DateTime.UtcNow - session.CurrentPickStartedAtUTC.Value).TotalSeconds;

		return Math.Max(0, PickSeconds - elapsedSeconds);
	}

	private async Task SendSystemAsync(Guid userId, string title, string body)
	{
		await _messageService.SendSystemMessageAsync(userId, title, body);
	}

	private static decimal GetDraftPrice(int? valueEur, int overall)
	{
		if (valueEur.HasValue && valueEur.Value > 0)
			return valueEur.Value;

		return overall switch
		{
			>= 90 => 100_000_000m,
			>= 86 => 60_000_000m,
			>= 83 => 30_000_000m,
			>= 80 => 15_000_000m,
			>= 75 => 5_000_000m,
			>= 70 => 1_000_000m,
			_ => 500_000m
		};
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
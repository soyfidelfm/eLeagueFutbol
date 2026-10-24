using backend.Data;
using backend.Entities.Clubs;
using backend.Entities.Draft;
using backend.Entities.Enums;
using backend.Entities.Transfers;
using backend.Features.Draft.Dtos;
using backend.Features.Messages.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.Draft.Services;

public class DraftService : IDraftService
{
	private readonly AppDbContext _context;
	private readonly IMessageService _messageService;

	/// <summary>Result of a timer sweep: which season changed and how ("skipped" or "completed").</summary>
	public record DraftTimerOutcome(Guid SeasonId, string EventType);

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
			var defaultPickSeconds = await GetDefaultPickSecondsAsync();

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
					PlayersPerTeam = 25,
					TimerEnabled = true,
					PickSeconds = defaultPickSeconds,
					ServerTimeUTC = DateTime.UtcNow
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

		if (session.IsCompleted)
			throw new Exception("This draft has already finished.");

		var order = await GetDraftOrderAsync(seasonId);

		if (order.Count == 0)
			throw new Exception("Draft order has not been generated.");

		var now = DateTime.UtcNow;

		session.IsActive = true;
		session.IsPaused = false;
		session.PausedRemainingSeconds = null;
		session.CurrentPickNumber = 1;
		session.CurrentPickStartedAtUTC = now;
		session.CurrentPickDeadlineUTC = session.TimerEnabled
			? now.AddSeconds(session.PickSeconds)
			: null;
		session.OneMinuteWarningSent = false;

		var firstTurn = GetCurrentTurn(session, order);

		if (firstTurn is null)
			throw new Exception("First draft turn not found.");

		await SendSystemAsync(
	firstTurn.UserId,
	"It's Your Turn!",
	"Choose wisely. Your next player could change everything."
);

		await _context.SaveChangesAsync();
		await transaction.CommitAsync();

		return await BuildDraftCenterAsync(seasonId, user.UserId, session);
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

		var now = DateTime.UtcNow;
		var pickSeconds = await GetDefaultPickSecondsAsync();

		var session = new DraftSession
		{
			DraftSessionId = Guid.NewGuid(),
			SeasonId = seasonId,
			IsActive = false,
			CurrentPickNumber = 1,
			PlayersPerTeam = 25,
			CurrentPickStartedAtUTC = null,
			OneMinuteWarningSent = false,
			TimerEnabled = true,
			PickSeconds = pickSeconds,
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

		await _context.SaveChangesAsync();
		await transaction.CommitAsync();

		return await BuildDraftCenterAsync(seasonId, user.UserId, session);
	}

	public async Task<List<DraftPlayerDto>> GetAvailablePlayersAsync(
	Guid seasonId,
	string? search,
	string? position,
	int page,
	int pageSize,
	DraftPlayerPoolFilter poolFilter = DraftPlayerPoolFilter.ClubOnly)
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

		var freeAgentsClubId = await _context.GameClubs
			.AsNoTracking()
			.Where(x => x.Name == "Free Agents")
			.Select(x => (Guid?)x.GameClubId)
			.FirstOrDefaultAsync();

		var query = _context.GamePlayers
			.AsNoTracking()
			.Include(x => x.GameClub)
			.Where(x =>
				!draftedPlayerIds.Contains(x.GamePlayerId) &&
				!x.IsDeleted &&
				x.IsActive);

		if (freeAgentsClubId.HasValue)
		{
			switch (poolFilter)
			{
				case DraftPlayerPoolFilter.ClubOnly:
					query = query.Where(x => x.GameClubId != freeAgentsClubId.Value);
					break;

				case DraftPlayerPoolFilter.FreeAgentsOnly:
					query = query.Where(x => x.GameClubId == freeAgentsClubId.Value);
					break;

				case DraftPlayerPoolFilter.IncludeFreeAgents:
					break;
			}
		}

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

		if (freeAgentsClubId.HasValue &&
			poolFilter == DraftPlayerPoolFilter.IncludeFreeAgents)
		{
			query = query
				.OrderBy(x => x.GameClubId == freeAgentsClubId.Value ? 1 : 0)
				.ThenByDescending(x =>
					x.GameClubId != freeAgentsClubId.Value
						? (x.ValueEur ?? 0)
						: 0)
				.ThenBy(x =>
					x.GameClubId == freeAgentsClubId.Value
						? (x.ValueEur ?? 0)
						: decimal.MaxValue)
				.ThenBy(x => x.Name);
		}
		else if (freeAgentsClubId.HasValue &&
				 poolFilter == DraftPlayerPoolFilter.FreeAgentsOnly)
		{
			query = query
				.OrderBy(x => x.ValueEur ?? 0)
				.ThenBy(x => x.Name);
		}
		else
		{
			query = query
				.OrderByDescending(x => x.ValueEur ?? 0)
				.ThenBy(x => x.Name);
		}

		return await query
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
				GameClubName = x.GameClub!.Name
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

		if (session.IsPaused)
			throw new Exception("The draft is paused.");

		var order = await GetDraftOrderAsync(seasonId);

		if (order.Count == 0)
			throw new Exception("Draft order has not been generated.");

		var currentTurn = GetCurrentTurn(session, order);

		if (currentTurn is null)
			throw new Exception("Current draft turn not found.");

		if (currentTurn.UserId != user.UserId)
			throw new Exception("It is not your turn.");

		var expectedPickNumber = session.CurrentPickNumber;

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

		await _context.SaveChangesAsync();

		// Advance only if the turn is still the one we validated; the timer sweep may have
		// skipped it a moment ago, in which case the whole pick is rolled back.
		var advanced = await TryAdvanceAsync(
			session,
			expectedPickNumber,
			order.Count * session.PlayersPerTeam,
			DateTime.UtcNow,
			onlyIfExpired: false);

		if (!advanced)
			throw new Exception("Your turn has expired.");

		await transaction.CommitAsync();

		await NotifyCurrentTurnAsync(seasonId, order);

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
			: GetSnakeOrderPosition(session.CurrentPickNumber, totalTeams);

		var currentTeam = order.FirstOrDefault(x => x.PickOrder == currentOrderPosition);

		foreach (var item in order)
			item.IsCurrentPick = session.IsActive && item.PickOrder == currentOrderPosition;

		var visibleDraftOrder = session.IsActive && currentRound % 2 == 0
			? order.OrderByDescending(x => x.PickOrder).ToList()
			: order.OrderBy(x => x.PickOrder).ToList();

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
				CurrentClubName = session.IsActive
	? currentTeam?.ClubName ?? string.Empty
	: "Draft order ready",
				SecondsRemaining = GetSecondsRemaining(session, DateTime.UtcNow),
				TotalTeams = totalTeams,
				PlayersPerTeam = session.PlayersPerTeam,
				IsPaused = session.IsPaused,
				IsCompleted = session.IsCompleted,
				TimerEnabled = session.TimerEnabled,
				PickSeconds = session.PickSeconds,
				CurrentPickDeadlineUTC = session.IsActive && !session.IsPaused && session.TimerEnabled
					? AsUtc(session.CurrentPickDeadlineUTC)
					: null,
				ServerTimeUTC = DateTime.UtcNow
			},
			DraftOrder = visibleDraftOrder,
			RecentPicks = picks.OrderByDescending(x => x.PickNumber).Take(10).ToList(),
			MySquad = new MyDraftSquadDto
			{
				TotalPlayers = mySquadPlayers.Count,
				Players = mySquadPlayers
			}
		};
	}

	/// <summary>Sends the one-minute warning for the current pick. Skipping expired picks is done by
	/// <see cref="ProcessExpiredPicksAsync"/> (run by DraftTimerBackgroundService) so it works with no clients open.</summary>
	private async Task ProcessDraftTimerAsync(DraftSession session)
	{
		if (!session.IsActive ||
			session.IsPaused ||
			!session.TimerEnabled ||
			session.CurrentPickDeadlineUTC is null ||
			session.OneMinuteWarningSent ||
			session.PickSeconds <= 60)
			return;

		var remaining = GetSecondsRemaining(session, DateTime.UtcNow);

		if (remaining > 60 || remaining <= 0)
			return;

		var order = await GetDraftOrderAsync(session.SeasonId);
		var currentTurn = GetCurrentTurn(session, order);

		if (currentTurn is null)
			return;

		var updated = await _context.DraftSessions
			.Where(x =>
				x.DraftSessionId == session.DraftSessionId &&
				x.CurrentPickNumber == session.CurrentPickNumber &&
				!x.OneMinuteWarningSent)
			.ExecuteUpdateAsync(s => s.SetProperty(x => x.OneMinuteWarningSent, true));

		session.OneMinuteWarningSent = true;

		if (updated == 1)
		{
			await SendSystemAsync(
				currentTurn.UserId,
				"Te queda 1 minuto",
				"Te queda 1 minuto para seleccionar tu pick del Draft.");
		}
	}

	/// <summary>
	/// Moves the draft from <paramref name="expectedPickNumber"/> to the next pick (or completes it) with a
	/// single conditional UPDATE, so a pick and the timer sweep can never both advance the same turn.
	/// </summary>
	private async Task<bool> TryAdvanceAsync(
		DraftSession session,
		int expectedPickNumber,
		int totalPicks,
		DateTime nowUtc,
		bool onlyIfExpired)
	{
		var timerEnabled = session.TimerEnabled;
		var pickSeconds = session.PickSeconds;

		var query = _context.DraftSessions.Where(x =>
			x.DraftSessionId == session.DraftSessionId &&
			x.CurrentPickNumber == expectedPickNumber &&
			x.IsActive &&
			!x.IsPaused &&
			x.TimerEnabled == timerEnabled &&
			x.PickSeconds == pickSeconds);

		if (onlyIfExpired)
		{
			query = query.Where(x =>
				x.TimerEnabled &&
				x.CurrentPickDeadlineUTC != null &&
				x.CurrentPickDeadlineUTC <= nowUtc);
		}

		int rows;

		if (expectedPickNumber >= totalPicks)
		{
			rows = await query.ExecuteUpdateAsync(s => s
				.SetProperty(x => x.IsActive, false)
				.SetProperty(x => x.IsCompleted, true)
				.SetProperty(x => x.CurrentPickStartedAtUTC, (DateTime?)null)
				.SetProperty(x => x.CurrentPickDeadlineUTC, (DateTime?)null)
				.SetProperty(x => x.OneMinuteWarningSent, false));
		}
		else
		{
			DateTime? deadline = timerEnabled ? nowUtc.AddSeconds(pickSeconds) : null;

			rows = await query.ExecuteUpdateAsync(s => s
				.SetProperty(x => x.CurrentPickNumber, expectedPickNumber + 1)
				.SetProperty(x => x.CurrentPickStartedAtUTC, nowUtc)
				.SetProperty(x => x.CurrentPickDeadlineUTC, deadline)
				.SetProperty(x => x.OneMinuteWarningSent, false));
		}

		return rows == 1;
	}

	private async Task NotifyCurrentTurnAsync(Guid seasonId, List<DraftOrder> order)
	{
		var fresh = await _context.DraftSessions
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.SeasonId == seasonId);

		if (fresh is null || !fresh.IsActive)
			return;

		var nextTurn = GetCurrentTurn(fresh, order);

		if (nextTurn is not null)
		{
			await SendSystemAsync(
				nextTurn.UserId,
				"It's Your Turn!",
				"Choose wisely. Your next player could change everything.");
		}
	}

	/// <summary>
	/// Skips every pick whose deadline has passed (no auto-pick logic exists, so the turn moves on).
	/// Safe to run from several instances at once: each skip is a conditional UPDATE.
	/// </summary>
	public async Task<List<DraftTimerOutcome>> ProcessExpiredPicksAsync(CancellationToken ct = default)
	{
		var now = DateTime.UtcNow;

		var expired = await _context.DraftSessions
			.AsNoTracking()
			.Where(x =>
				x.IsActive &&
				!x.IsPaused &&
				x.TimerEnabled &&
				x.CurrentPickDeadlineUTC != null &&
				x.CurrentPickDeadlineUTC <= now)
			.ToListAsync(ct);

		var outcomes = new List<DraftTimerOutcome>();

		foreach (var session in expired)
		{
			var order = await GetDraftOrderAsync(session.SeasonId);

			if (order.Count == 0)
				continue;

			var skippedTurn = GetCurrentTurn(session, order);
			var totalPicks = order.Count * session.PlayersPerTeam;
			var wasLastPick = session.CurrentPickNumber >= totalPicks;

			var advanced = await TryAdvanceAsync(
				session,
				session.CurrentPickNumber,
				totalPicks,
				now,
				onlyIfExpired: true);

			if (!advanced)
				continue;

			if (skippedTurn is not null)
			{
				await SendSystemAsync(
					skippedTurn.UserId,
					"Tu turno terminó",
					"Se acabó tu tiempo para seleccionar. Tu turno fue saltado.");
			}

			if (!wasLastPick)
				await NotifyCurrentTurnAsync(session.SeasonId, order);

			outcomes.Add(new DraftTimerOutcome(
				session.SeasonId,
				wasLastPick ? "completed" : "skipped"));
		}

		return outcomes;
	}

	// ===== Admin draft controls =====

	public async Task<DraftCenterDto> PauseDraftAsync(Guid seasonId, string auth0Id)
	{
		var admin = await EnsureAdminUserAsync(auth0Id);
		var session = await GetSessionOrThrowAsync(seasonId);

		if (!session.IsActive)
			throw new Exception("Draft is not active.");

		if (session.IsPaused)
			throw new Exception("The draft is already paused.");

		var now = DateTime.UtcNow;
		int? remaining = session.TimerEnabled ? GetSecondsRemaining(session, now) : null;

		var rows = await _context.DraftSessions
			.Where(x =>
				x.DraftSessionId == session.DraftSessionId &&
				x.CurrentPickNumber == session.CurrentPickNumber &&
				x.IsActive &&
				!x.IsPaused)
			.ExecuteUpdateAsync(s => s
				.SetProperty(x => x.IsPaused, true)
				.SetProperty(x => x.PausedRemainingSeconds, remaining)
				.SetProperty(x => x.CurrentPickDeadlineUTC, (DateTime?)null));

		if (rows != 1)
			throw new Exception("The draft changed while pausing. Please try again.");

		return await BuildFreshCenterAsync(seasonId, admin.UserId);
	}

	public async Task<DraftCenterDto> ResumeDraftAsync(Guid seasonId, string auth0Id)
	{
		var admin = await EnsureAdminUserAsync(auth0Id);
		var session = await GetSessionOrThrowAsync(seasonId);

		if (!session.IsActive || !session.IsPaused)
			throw new Exception("The draft is not paused.");

		var now = DateTime.UtcNow;
		var seconds = Math.Max(1, session.PausedRemainingSeconds ?? session.PickSeconds);
		DateTime? deadline = session.TimerEnabled ? now.AddSeconds(seconds) : null;

		var rows = await _context.DraftSessions
			.Where(x =>
				x.DraftSessionId == session.DraftSessionId &&
				x.IsActive &&
				x.IsPaused)
			.ExecuteUpdateAsync(s => s
				.SetProperty(x => x.IsPaused, false)
				.SetProperty(x => x.PausedRemainingSeconds, (int?)null)
				.SetProperty(x => x.CurrentPickDeadlineUTC, deadline));

		if (rows != 1)
			throw new Exception("The draft changed while resuming. Please try again.");

		return await BuildFreshCenterAsync(seasonId, admin.UserId);
	}

	public async Task<DraftCenterDto> StopDraftAsync(Guid seasonId, string auth0Id)
	{
		var admin = await EnsureAdminUserAsync(auth0Id);
		var session = await GetSessionOrThrowAsync(seasonId);

		if (!session.IsActive)
			throw new Exception("Draft is not active.");

		var rows = await _context.DraftSessions
			.Where(x => x.DraftSessionId == session.DraftSessionId && x.IsActive)
			.ExecuteUpdateAsync(s => s
				.SetProperty(x => x.IsActive, false)
				.SetProperty(x => x.IsPaused, false)
				.SetProperty(x => x.IsCompleted, true)
				.SetProperty(x => x.PausedRemainingSeconds, (int?)null)
				.SetProperty(x => x.CurrentPickStartedAtUTC, (DateTime?)null)
				.SetProperty(x => x.CurrentPickDeadlineUTC, (DateTime?)null));

		if (rows != 1)
			throw new Exception("The draft changed while stopping. Please try again.");

		return await BuildFreshCenterAsync(seasonId, admin.UserId);
	}

	public async Task<DraftCenterDto> UpdateTimerAsync(
		Guid seasonId,
		string auth0Id,
		UpdateDraftTimerDto dto)
	{
		var admin = await EnsureAdminUserAsync(auth0Id);
		ValidatePickSeconds(dto.PickSeconds);

		var session = await GetSessionOrThrowAsync(seasonId);

		if (session.IsCompleted)
			throw new Exception("This draft has already finished.");

		var now = DateTime.UtcNow;
		var running = session.IsActive && !session.IsPaused;

		// A running pick restarts with the new settings; a paused pick resumes with a full new clock.
		DateTime? deadline = running && dto.TimerEnabled ? now.AddSeconds(dto.PickSeconds) : null;
		int? pausedRemaining = session.IsPaused && dto.TimerEnabled ? dto.PickSeconds : null;
		DateTime? startedAt = running ? now : session.CurrentPickStartedAtUTC;

		var rows = await _context.DraftSessions
			.Where(x =>
				x.DraftSessionId == session.DraftSessionId &&
				x.CurrentPickNumber == session.CurrentPickNumber &&
				x.IsActive == session.IsActive &&
				x.IsPaused == session.IsPaused)
			.ExecuteUpdateAsync(s => s
				.SetProperty(x => x.TimerEnabled, dto.TimerEnabled)
				.SetProperty(x => x.PickSeconds, dto.PickSeconds)
				.SetProperty(x => x.CurrentPickDeadlineUTC, deadline)
				.SetProperty(x => x.PausedRemainingSeconds, pausedRemaining)
				.SetProperty(x => x.CurrentPickStartedAtUTC, startedAt)
				.SetProperty(x => x.OneMinuteWarningSent, false));

		if (rows != 1)
			throw new Exception("The draft changed while saving the timer. Please try again.");

		return await BuildFreshCenterAsync(seasonId, admin.UserId);
	}

	// ===== League-wide draft settings =====

	public async Task<DraftSettingsDto> GetDraftSettingsAsync()
	{
		return new DraftSettingsDto
		{
			DefaultPickSeconds = await GetDefaultPickSecondsAsync()
		};
	}

	public async Task<DraftSettingsDto> UpdateDraftSettingsAsync(string auth0Id, DraftSettingsDto dto)
	{
		await EnsureAdminUserAsync(auth0Id);
		ValidatePickSeconds(dto.DefaultPickSeconds);

		var setting = await _context.AppSettings
			.FirstOrDefaultAsync(x => x.Key == DraftTimerLimits.DefaultPickSecondsKey);

		if (setting is null)
		{
			setting = new backend.Entities.Common.AppSetting
			{
				Key = DraftTimerLimits.DefaultPickSecondsKey
			};

			_context.AppSettings.Add(setting);
		}

		setting.Value = dto.DefaultPickSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
		setting.UpdatedAtUTC = DateTime.UtcNow;

		await _context.SaveChangesAsync();

		return await GetDraftSettingsAsync();
	}

	private async Task<int> GetDefaultPickSecondsAsync()
	{
		var value = await _context.AppSettings
			.AsNoTracking()
			.Where(x => x.Key == DraftTimerLimits.DefaultPickSecondsKey)
			.Select(x => x.Value)
			.FirstOrDefaultAsync();

		return int.TryParse(value, out var seconds) &&
			seconds >= DraftTimerLimits.MinPickSeconds &&
			seconds <= DraftTimerLimits.MaxPickSeconds
				? seconds
				: DraftTimerLimits.DefaultPickSeconds;
	}

	private static void ValidatePickSeconds(int seconds)
	{
		if (seconds < DraftTimerLimits.MinPickSeconds || seconds > DraftTimerLimits.MaxPickSeconds)
			throw new ArgumentException(
				$"Seconds per pick must be between {DraftTimerLimits.MinPickSeconds} and {DraftTimerLimits.MaxPickSeconds}.");
	}

	private async Task<backend.Entities.Identity.User> EnsureAdminUserAsync(string auth0Id)
	{
		var user = await _context.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted &&
				x.IsActive)
			?? throw new UnauthorizedAccessException("User not found.");

		if (!string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
			throw new UnauthorizedAccessException("Only admins can control the draft.");

		return user;
	}

	private async Task<DraftSession> GetSessionOrThrowAsync(Guid seasonId)
	{
		return await _context.DraftSessions
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.SeasonId == seasonId)
			?? throw new Exception("Draft order has not been generated.");
	}

	private async Task<DraftCenterDto> BuildFreshCenterAsync(Guid seasonId, Guid userId)
	{
		var session = await GetSessionOrThrowAsync(seasonId);
		return await BuildDraftCenterAsync(seasonId, userId, session);
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

		var currentOrderPosition = GetSnakeOrderPosition(
			session.CurrentPickNumber,
			order.Count);

		return order.FirstOrDefault(x => x.PickOrder == currentOrderPosition);
	}

	private static int GetSnakeOrderPosition(int currentPickNumber, int totalTeams)
	{
		if (totalTeams <= 0)
			return 0;

		var roundNumber = ((currentPickNumber - 1) / totalTeams) + 1;
		var positionInRound = ((currentPickNumber - 1) % totalTeams) + 1;

		var isOddRound = roundNumber % 2 == 1;

		return isOddRound
			? positionInRound
			: totalTeams - positionInRound + 1;
	}

	private static int GetSecondsRemaining(DraftSession session, DateTime nowUtc)
	{
		if (!session.IsActive)
			return 0;

		if (session.IsPaused)
			return Math.Max(0, session.PausedRemainingSeconds ?? 0);

		var deadline = AsUtc(session.CurrentPickDeadlineUTC);

		if (!session.TimerEnabled || deadline is null)
			return 0;

		return Math.Max(0, (int)Math.Ceiling((deadline.Value - nowUtc).TotalSeconds));
	}

	private static DateTime? AsUtc(DateTime? value)
	{
		return value is null ? null : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
	}

	private async Task SendSystemAsync(Guid userId, string title, string body)
	{
		await _messageService.SendSystemMessageAsync(userId, title, body, "draft_lottery", "/draft");
	}

	private static decimal GetDraftPrice(int? valueEur, int overall)
	{
		if (valueEur.HasValue)
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

using backend.Data;
using backend.Entities.Clubs;
using backend.Entities.Draft;
using backend.Entities.Transfers;
using backend.Features.Draft.Dtos;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.Draft.Services;

public class DraftService : IDraftService
{
	private readonly AppDbContext _context;

	public DraftService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<DraftCenterDto?> GetCenterAsync(Guid seasonId, string auth0Id)
	{
		var user = await _context.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Auth0Id == auth0Id && !x.IsDeleted);

		if (user is null)
			return null;

		var session = await EnsureDraftSessionAsync(seasonId);
		await EnsureDraftOrderAsync(seasonId);

		var userClubs = await _context.UserClubs
			.AsNoTracking()
			.Include(x => x.BaseGameClub)
			.Where(x => x.SeasonId == seasonId && !x.IsDeleted)
			.ToDictionaryAsync(x => x.UserId);

		var myClub = userClubs.TryGetValue(user.UserId, out var foundClub)
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
			? 1
			: ((session.CurrentPickNumber - 1) / totalTeams) + 1;

		var currentOrderPosition = totalTeams == 0
			? 0
			: ((session.CurrentPickNumber - 1) % totalTeams) + 1;

		var currentTeam = order.FirstOrDefault(x => x.PickOrder == currentOrderPosition);

		foreach (var item in order)
			item.IsCurrentPick = item.PickOrder == currentOrderPosition;

		var mySquadPlayers = picks
			.Where(x => x.UserId == user.UserId)
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
			MyUserId = user.UserId,
			CurrentBudget = myClub?.CurrentBudget ?? 0,

			State = new DraftStateDto
			{
				DraftSessionId = session.DraftSessionId,
				IsActive = session.IsActive,
				CurrentRound = currentRound,
				CurrentPickNumber = session.CurrentPickNumber,
				CurrentUserId = currentTeam?.UserId,
				CurrentClubName = currentTeam?.ClubName ?? string.Empty,
				SecondsRemaining = 0,
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

	public async Task<List<DraftPlayerDto>> GetAvailablePlayersAsync(
		Guid seasonId,
		string? search,
		string? position,
		int page,
		int pageSize)
	{
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

		var order = await _context.DraftOrders
			.AsNoTracking()
			.Where(x => x.SeasonId == seasonId)
			.OrderBy(x => x.PickOrder)
			.ToListAsync();

		if (order.Count == 0)
			throw new Exception("Draft order has not been generated.");

		var currentOrderPosition = ((session.CurrentPickNumber - 1) % order.Count) + 1;

		var currentTurn = order.FirstOrDefault(x => x.PickOrder == currentOrderPosition);

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
		session.CurrentPickNumber++;

		await _context.SaveChangesAsync();
		await transaction.CommitAsync();

		var result = await GetPicksAsync(seasonId);

		return result.First(x => x.PickNumber == pick.PickNumber);
	}

	private async Task<DraftSession> EnsureDraftSessionAsync(Guid seasonId)
	{
		var session = await _context.DraftSessions
			.FirstOrDefaultAsync(x => x.SeasonId == seasonId);

		if (session is not null)
			return session;

		session = new DraftSession
		{
			DraftSessionId = Guid.NewGuid(),
			SeasonId = seasonId,
			IsActive = true,
			CurrentPickNumber = 1,
			PlayersPerTeam = 25,
			CreatedAtUTC = DateTime.UtcNow
		};

		_context.DraftSessions.Add(session);

		await _context.SaveChangesAsync();

		return session;
	}

	private async Task EnsureDraftOrderAsync(Guid seasonId)
	{
		var alreadyExists = await _context.DraftOrders
			.AnyAsync(x => x.SeasonId == seasonId);

		if (alreadyExists)
			return;

		var registrations = await _context.SeasonRegistrations
			.AsNoTracking()
			.Include(x => x.User)
			.Where(x =>
				x.SeasonId == seasonId &&
				!x.User.IsDeleted)
			.OrderBy(x => x.RegisteredAtUTC)
			.ToListAsync();

		if (registrations.Count == 0)
			return;

		var draftOrders = registrations
			.Select((registration, index) => new DraftOrder
			{
				DraftOrderId = Guid.NewGuid(),
				SeasonId = seasonId,
				UserId = registration.UserId,
				PickOrder = index + 1
			})
			.ToList();

		_context.DraftOrders.AddRange(draftOrders);

		await _context.SaveChangesAsync();
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
}
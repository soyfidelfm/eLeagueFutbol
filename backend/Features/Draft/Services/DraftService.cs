using backend.Data;
using backend.Entities.Draft;
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
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted);

		if (user is null)
			return null;

		var session = await EnsureDraftSessionAsync(seasonId);

		await EnsureDraftOrderAsync(seasonId);

		var order = await _context.DraftOrders
			.AsNoTracking()
			.Include(x => x.User)
			.ThenInclude(x => x.GameClub)
			.Where(x => x.SeasonId == seasonId)
			.OrderBy(x => x.PickOrder)
			.Select(x => new DraftOrderDto
			{
				PickOrder = x.PickOrder,
				UserId = x.UserId,
				ClubName = x.User.ClubName ?? x.User.DisplayName,
				LogoUrl = x.User.GameClub != null ? x.User.GameClub.LogoUrl : null,
				IsCurrentPick = false
			})
			.ToListAsync();

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
			RecentPicks = picks
				.OrderByDescending(x => x.PickNumber)
				.Take(10)
				.ToList(),
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
			.Where(x => !draftedPlayerIds.Contains(x.GamePlayerId));

		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = search.Trim().ToLower();

			query = query.Where(x =>
				x.Name.ToLower().Contains(term) ||
				x.FullName.ToLower().Contains(term));
		}

		if (!string.IsNullOrWhiteSpace(position))
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
				Name = x.Name,
				Position = x.Position,
				Overall = x.Overall,
				Potential = x.Potential,
				Nation = x.Nation,
				Age = x.Age,
				ImageUrl = x.ImageUrl,
				GameClubName = x.GameClub.Name
			})
			.ToListAsync();
	}

	public async Task<List<DraftPickDto>> GetPicksAsync(Guid seasonId)
	{
		return await _context.DraftPicks
			.AsNoTracking()
			.Include(x => x.User)
			.Include(x => x.GamePlayer)
			.Where(x => x.SeasonId == seasonId)
			.OrderBy(x => x.PickNumber)
			.Select(x => new DraftPickDto
			{
				PickNumber = x.PickNumber,
				RoundNumber = x.RoundNumber,
				UserId = x.UserId,
				ClubName = x.User.ClubName ?? x.User.DisplayName,
				GamePlayerId = x.GamePlayerId,
				PlayerName = x.GamePlayer.Name,
				Position = x.GamePlayer.Position,
				Overall = x.GamePlayer.Overall,
				PlayerImageUrl = x.GamePlayer.ImageUrl,
				PickedAtUTC = x.PickedAtUTC
			})
			.ToListAsync();
	}

	public async Task<DraftPickDto> MakePickAsync(
		Guid seasonId,
		string auth0Id,
		MakeDraftPickDto dto)
	{
		var user = await _context.Users
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted);

		if (user is null)
			throw new Exception("User not found.");

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

		var currentTurn = order.FirstOrDefault(x =>
			x.PickOrder == currentOrderPosition);

		if (currentTurn is null)
			throw new Exception("Current draft turn not found.");

		if (currentTurn.UserId != user.UserId)
			throw new Exception("It is not your turn.");

		var alreadyPicked = await _context.DraftPicks
			.AnyAsync(x =>
				x.SeasonId == seasonId &&
				x.GamePlayerId == dto.GamePlayerId);

		if (alreadyPicked)
			throw new Exception("This player has already been drafted.");

		var playerExists = await _context.GamePlayers
			.AnyAsync(x => x.GamePlayerId == dto.GamePlayerId);

		if (!playerExists)
			throw new Exception("Player not found.");

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

		session.CurrentPickNumber++;

		await _context.SaveChangesAsync();

		var result = await _context.DraftPicks
			.AsNoTracking()
			.Include(x => x.User)
			.Include(x => x.GamePlayer)
			.Where(x => x.DraftPickId == pick.DraftPickId)
			.Select(x => new DraftPickDto
			{
				PickNumber = x.PickNumber,
				RoundNumber = x.RoundNumber,
				UserId = x.UserId,
				ClubName = x.User.ClubName ?? x.User.DisplayName,
				GamePlayerId = x.GamePlayerId,
				PlayerName = x.GamePlayer.Name,
				Position = x.GamePlayer.Position,
				Overall = x.GamePlayer.Overall,
				PlayerImageUrl = x.GamePlayer.ImageUrl,
				PickedAtUTC = x.PickedAtUTC
			})
			.FirstAsync();

		return result;
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
			PlayersPerTeam = 11,
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
}
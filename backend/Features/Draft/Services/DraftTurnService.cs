using backend.Data;
using backend.Entities.Clubs;
using backend.Entities.Draft;
using backend.Features.Draft.Dtos;
using backend.Features.Draft.Hubs;
using backend.Features.Messages.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.Draft.Services;

public class DraftTurnService : IDraftTurnService
{
	private readonly AppDbContext _context;
	private readonly IMessageService _messageService;
	private readonly IHubContext<DraftHub> _draftHub;

	private const int PickSeconds = 600; // 10 minutes

	public DraftTurnService(
		AppDbContext context,
		IMessageService messageService,
		IHubContext<DraftHub> draftHub)
	{
		_context = context;
		_messageService = messageService;
		_draftHub = draftHub;
	}

	public async Task<DraftCenterDto> BuildDraftCenterAsync(
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

		var currentPickAsset = session.IsActive
			? await GetCurrentPickAssetAsync(session)
			: null;

		var currentOwnerClub = currentPickAsset?.CurrentOwnerUserClub;

		foreach (var item in order)
		{
			item.IsCurrentPick =
				session.IsActive &&
				currentOwnerClub is not null &&
				item.UserId == currentOwnerClub.UserId;
		}

		var totalTeams = order.Count;

		var currentRound = currentPickAsset?.RoundNumber
			?? (totalTeams == 0 ? 0 : ((session.CurrentPickNumber - 1) / totalTeams) + 1);

		var visibleDraftOrder = session.IsActive && currentRound % 2 == 0
			? order.OrderByDescending(x => x.PickOrder).ToList()
			: order.OrderBy(x => x.PickOrder).ToList();

		var picks = await GetPicksAsync(seasonId);

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
				CurrentUserId = session.IsActive ? currentOwnerClub?.UserId : null,
				CurrentClubName = session.IsActive
					? currentOwnerClub?.Name ?? string.Empty
					: "Draft order ready",
				SecondsRemaining = GetSecondsRemaining(session),
				TotalTeams = totalTeams,
				PlayersPerTeam = session.PlayersPerTeam
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

	public async Task ProcessDraftTimerAsync(DraftSession session)
	{
		if (!session.IsActive || session.CurrentPickStartedAtUTC is null)
			return;

		var currentPickAsset = await GetCurrentPickAssetAsync(session);

		if (currentPickAsset is null)
			return;

		var currentClub = currentPickAsset.CurrentOwnerUserClub;

		var remaining = GetSecondsRemaining(session);

		if (remaining <= 60 && !session.OneMinuteWarningSent && remaining > 0)
		{
			session.OneMinuteWarningSent = true;

			await SendSystemAsync(
				currentClub.UserId,
				"Te queda 1 minuto",
				"Te queda 1 minuto para seleccionar tu pick del Draft.");
		}

		if (remaining <= 0)
		{
			await SendSystemAsync(
				currentClub.UserId,
				"Tu turno terminó",
				"Se acabó tu tiempo para seleccionar. Tu turno fue saltado.");

			currentPickAsset.IsUsed = true;
			currentPickAsset.UpdatedAtUTC = DateTime.UtcNow;

			var order = await GetDraftOrderAsync(session.SeasonId);

			await AdvanceDraftTurnAsync(session, order);
		}

		await _context.SaveChangesAsync();
	}

	public async Task AdvanceDraftTurnAsync(
		DraftSession session,
		List<DraftOrder> order)
	{
		var totalPicks = await _context.DraftPickAssets
			.AsNoTracking()
			.CountAsync(x => x.SeasonId == session.SeasonId);

		if (totalPicks == 0)
			totalPicks = order.Count * session.PlayersPerTeam;

		if (session.CurrentPickNumber >= totalPicks)
		{
			session.IsActive = false;
			session.CurrentPickStartedAtUTC = null;
			session.OneMinuteWarningSent = false;

			await _draftHub.Clients
				.Group($"draft-{session.SeasonId}")
				.SendAsync("DraftCompleted", new
				{
					seasonId = session.SeasonId
				});

			return;
		}

		session.CurrentPickNumber++;
		session.CurrentPickStartedAtUTC = DateTime.UtcNow;
		session.OneMinuteWarningSent = false;

		var nextPickAsset = await GetCurrentPickAssetAsync(session);

		if (nextPickAsset is null)
		{
			session.IsActive = false;
			session.CurrentPickStartedAtUTC = null;
			session.OneMinuteWarningSent = false;

			await _draftHub.Clients
				.Group($"draft-{session.SeasonId}")
				.SendAsync("DraftCompleted", new
				{
					seasonId = session.SeasonId
				});

			return;
		}

		var nextClub = nextPickAsset.CurrentOwnerUserClub;

		await SendSystemAsync(
			nextClub.UserId,
			"It's Your Turn to Draft",
			"You are now on the clock. You have 10 minutes to make your selection.");

		await _draftHub.Clients
			.Group($"draft-{session.SeasonId}")
			.SendAsync("DraftTurnStarted", new
			{
				seasonId = session.SeasonId,
				pickNumber = session.CurrentPickNumber,
				roundNumber = nextPickAsset.RoundNumber,
				clubName = nextClub.Name,
				logoUrl = nextClub.LogoUrl,
				userId = nextClub.UserId,
				userClubId = nextClub.UserClubId
			});
	}

	public async Task<List<DraftOrder>> GetDraftOrderAsync(Guid seasonId)
	{
		return await _context.DraftOrders
			.AsNoTracking()
			.Where(x => x.SeasonId == seasonId)
			.OrderBy(x => x.PickOrder)
			.ToListAsync();
	}

	public DraftOrder? GetCurrentTurn(
		DraftSession session,
		List<DraftOrder> order)
	{
		if (order.Count == 0)
			return null;

		var currentOrderPosition = GetSnakeOrderPosition(
			session.CurrentPickNumber,
			order.Count);

		return order.FirstOrDefault(x => x.PickOrder == currentOrderPosition);
	}

	public async Task<UserClub?> GetCurrentPickOwnerClubAsync(DraftSession session)
	{
		var currentPickAsset = await GetCurrentPickAssetAsync(session);

		return currentPickAsset?.CurrentOwnerUserClub;
	}

	public int GetSnakeOrderPosition(
		int currentPickNumber,
		int totalTeams)
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

	public int GetSecondsRemaining(DraftSession session)
	{
		if (!session.IsActive || session.CurrentPickStartedAtUTC is null)
			return 0;

		var elapsedSeconds = (int)(DateTime.UtcNow - session.CurrentPickStartedAtUTC.Value).TotalSeconds;

		return Math.Max(0, PickSeconds - elapsedSeconds);
	}

	public decimal GetDraftPrice(int? valueEur, int overall)
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

	private async Task<DraftPickAsset?> GetCurrentPickAssetAsync(DraftSession session)
	{
		return await _context.DraftPickAssets
			.Include(x => x.CurrentOwnerUserClub)
			.FirstOrDefaultAsync(x =>
				x.SeasonId == session.SeasonId &&
				x.PickNumber == session.CurrentPickNumber &&
				!x.IsUsed);
	}

	private async Task<List<DraftPickDto>> GetPicksAsync(Guid seasonId)
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

	private async Task SendSystemAsync(
		Guid userId,
		string title,
		string body)
	{
		await _messageService.SendSystemMessageAsync(userId, title, body);
	}
}
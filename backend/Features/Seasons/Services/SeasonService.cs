using backend.Common.Helpers;
using backend.Data;
using backend.Entities.Clubs;
using backend.Entities.Tournaments;
using backend.Entities.Transfers;
using backend.Features.Seasons.Dtos;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.Seasons.Services;

public class SeasonService : ISeasonService
{
	private const decimal InitialClubBudget = 350_000_000m;
	private const string OfferPending = "OfferPending";
	private const string OfferCancelled = "OfferCancelled";

	private readonly AppDbContext _db;

	public SeasonService(AppDbContext db)
	{
		_db = db;
	}

	public async Task<IReadOnlyList<SeasonDto>> GetAllAsync(CancellationToken ct)
	{
		var seasons = await _db.Seasons
			.AsNoTracking()
			.Where(x => !x.IsDeleted)
			.OrderByDescending(x => x.StartDate)
			.ToListAsync(ct);

		return await EnrichAsync(seasons, ct);
	}

	public async Task<SeasonDto?> GetByIdAsync(Guid id, CancellationToken ct)
	{
		var season = await _db.Seasons
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.SeasonId == id && !x.IsDeleted, ct);

		if (season is null)
			return null;

		var list = await EnrichAsync([season], ct);
		return list[0];
	}

	public async Task<SeasonDto?> GetCurrentAsync(CancellationToken ct)
	{
		var season = await _db.Seasons
			.AsNoTracking()
			.Where(x => !x.IsDeleted && x.IsCurrent)
			.OrderByDescending(x => x.StartDate)
			.FirstOrDefaultAsync(ct);

		if (season is null)
			return null;

		var list = await EnrichAsync([season], ct);
		return list[0];
	}

	public async Task<SeasonDto> CreateAsync(CreateSeasonDto dto, CancellationToken ct)
	{
		var season = new Season
		{
			SeasonId = Guid.NewGuid(),
			Name = dto.Name.Trim(),
			Description = dto.Description?.Trim() ?? string.Empty,
			StartDate = DateTimeHelper.ToUtc(dto.StartDate, dto.TimeZone),
			EndDate = DateTimeHelper.ToUtc(dto.EndDate, dto.TimeZone),
			IsCurrent = dto.IsCurrent,
			IsActive = dto.IsActive,
			TotalWeeks = dto.TotalWeeks,
			RegistrationOpen = dto.RegistrationOpen,
			MaxPlayers = dto.MaxPlayers
		};

		_db.Seasons.Add(season);

		if (season.IsCurrent)
			await ClearOtherCurrentAsync(season.SeasonId, ct);

		await _db.SaveChangesAsync(ct);

		var list = await EnrichAsync([season], ct);
		return list[0];
	}

	public async Task<bool> UpdateAsync(Guid id, UpdateSeasonDto dto, CancellationToken ct)
	{
		var season = await _db.Seasons
			.FirstOrDefaultAsync(x => x.SeasonId == id && !x.IsDeleted, ct);

		if (season is null)
			return false;

		season.Name = dto.Name.Trim();
		season.Description = dto.Description?.Trim() ?? string.Empty;
		season.StartDate = DateTimeHelper.ToUtc(dto.StartDate, dto.TimeZone);
		season.EndDate = DateTimeHelper.ToUtc(dto.EndDate, dto.TimeZone);
		season.IsCurrent = dto.IsCurrent;
		season.IsActive = dto.IsActive;
		season.TotalWeeks = dto.TotalWeeks;
		season.RegistrationOpen = dto.RegistrationOpen;
		season.MaxPlayers = dto.MaxPlayers;

		if (!season.IsActive)
			season.RegistrationOpen = false;

		if (season.IsCurrent)
			await ClearOtherCurrentAsync(season.SeasonId, ct);

		await _db.SaveChangesAsync(ct);

		return true;
	}

	public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
	{
		var season = await _db.Seasons
			.FirstOrDefaultAsync(x => x.SeasonId == id && !x.IsDeleted, ct);

		if (season is null)
			return false;

		season.IsDeleted = true;
		season.IsCurrent = false;
		season.IsActive = false;
		season.RegistrationOpen = false;

		await _db.SaveChangesAsync(ct);

		return true;
	}

	public async Task<SeasonRegistrationDto> RegisterAsync(
		Guid seasonId,
		RegisterSeasonDto dto,
		string auth0Id,
		CancellationToken ct)
	{
		var user = await _db.Users
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				x.IsActive &&
				!x.IsDeleted,
				ct);

		if (user is null)
			throw new Exception("User not found.");

		var season = await _db.Seasons
			.FirstOrDefaultAsync(x =>
				x.SeasonId == seasonId &&
				!x.IsDeleted,
				ct);

		if (season is null)
			throw new Exception("Season not found.");

		if (!season.IsActive)
			throw new Exception("Season is not active.");

		if (!season.RegistrationOpen)
			throw new Exception("Registration is closed.");

		var draftStarted = await _db.DraftSessions
			.AnyAsync(x => x.SeasonId == seasonId && x.CurrentPickStartedAtUTC != null, ct);

		if (draftStarted)
			throw new Exception("Registration closed because the draft has started.");

		var gameClubExists = await _db.GameClubs
			.AnyAsync(x =>
				x.GameClubId == dto.GameClubId &&
				!x.IsDeleted &&
				x.IsActive,
				ct);

		if (!gameClubExists)
			throw new Exception("Game club not found.");

		var alreadyRegistered = await _db.SeasonRegistrations
			.AnyAsync(x =>
				x.SeasonId == seasonId &&
				x.UserId == user.UserId,
				ct);

		if (alreadyRegistered)
			throw new Exception("User already registered.");

		var totalRegistered = await _db.SeasonRegistrations
			.CountAsync(x => x.SeasonId == seasonId, ct);

		if (totalRegistered >= season.MaxPlayers)
			throw new Exception("Season is full.");

		var registration = new SeasonRegistration
		{
			SeasonRegistrationId = Guid.NewGuid(),
			SeasonId = seasonId,
			UserId = user.UserId,
			GameClubId = dto.GameClubId,
			RegisteredAtUTC = DateTime.UtcNow
		};

		_db.SeasonRegistrations.Add(registration);

		var clubName = string.IsNullOrWhiteSpace(user.ClubName)
			? user.DisplayName
			: user.ClubName.Trim();

		if (string.IsNullOrWhiteSpace(clubName))
			clubName = "My Club";

		var userClub = new UserClub
		{
			UserClubId = Guid.NewGuid(),
			UserId = user.UserId,
			SeasonId = seasonId,
			BaseGameClubId = dto.GameClubId,
			Name = clubName,
			LogoUrl = string.Empty,
			InitialBudget = InitialClubBudget,
			CurrentBudget = InitialClubBudget
		};

		_db.UserClubs.Add(userClub);

		_db.UserClubBudgetTransactions.Add(new UserClubBudgetTransaction
		{
			UserClubBudgetTransactionId = Guid.NewGuid(),
			UserClubId = userClub.UserClubId,
			SeasonId = seasonId,
			Amount = InitialClubBudget,
			Type = "InitialBudget",
			Description = $"Initial budget assigned: {InitialClubBudget:N0}"
		});

		await _db.SaveChangesAsync(ct);

		return new SeasonRegistrationDto
		{
			SeasonRegistrationId = registration.SeasonRegistrationId,
			SeasonId = registration.SeasonId,
			UserId = registration.UserId,
			GameClubId = registration.GameClubId,
			RegisteredAtUTC = registration.RegisteredAtUTC
		};
	}

	public async Task<bool> IsUserRegisteredAsync(
		Guid seasonId,
		string auth0Id,
		CancellationToken ct)
	{
		var user = await _db.Users
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				x.IsActive &&
				!x.IsDeleted,
				ct);

		if (user is null)
			return false;

		return await _db.SeasonRegistrations
			.AnyAsync(x =>
				x.SeasonId == seasonId &&
				x.UserId == user.UserId,
				ct);
	}

	public async Task<SeasonDto> CloseRegistrationAsync(Guid id, CancellationToken ct)
	{
		var season = await GetTrackedSeasonAsync(id, ct);

		season.RegistrationOpen = false;
		await _db.SaveChangesAsync(ct);

		var list = await EnrichAsync([season], ct);
		return list[0];
	}

	public async Task<SeasonDto> OpenRegistrationAsync(Guid id, CancellationToken ct)
	{
		var season = await GetTrackedSeasonAsync(id, ct);

		if (!season.IsActive)
			throw new InvalidOperationException("Activate the season before opening registration.");

		var draftStarted = await _db.DraftSessions
			.AnyAsync(x => x.SeasonId == id && x.CurrentPickStartedAtUTC != null, ct);

		if (draftStarted)
			throw new InvalidOperationException("Cannot reopen registration after the draft has started.");

		var hasSchedule = await SeasonHasScheduleAsync(id, ct);

		if (hasSchedule)
			throw new InvalidOperationException("Cannot reopen registration after the fixture exists.");

		season.RegistrationOpen = true;
		await _db.SaveChangesAsync(ct);

		var list = await EnrichAsync([season], ct);
		return list[0];
	}

	public async Task<SeasonDto> SetCurrentAsync(Guid id, CancellationToken ct)
	{
		var season = await GetTrackedSeasonAsync(id, ct);

		season.IsCurrent = true;
		season.IsActive = true;

		await ClearOtherCurrentAsync(season.SeasonId, ct);
		await _db.SaveChangesAsync(ct);

		var list = await EnrichAsync([season], ct);
		return list[0];
	}

	public async Task<SeasonDto> CloseSeasonAsync(Guid id, CancellationToken ct)
	{
		var season = await GetTrackedSeasonAsync(id, ct);

		season.IsActive = false;
		season.IsCurrent = false;
		season.RegistrationOpen = false;

		var pending = await _db.TransferRecords
			.Where(x =>
				x.SeasonId == id &&
				!x.IsDeleted &&
				x.TransferType == OfferPending)
			.ToListAsync(ct);

		foreach (var offer in pending)
		{
			offer.TransferType = OfferCancelled;
			offer.Notes = "Cancelled because the season was closed.";
		}

		await _db.SaveChangesAsync(ct);

		var list = await EnrichAsync([season], ct);
		return list[0];
	}

	public async Task<SeasonDto> ActivateSeasonAsync(Guid id, CancellationToken ct)
	{
		var season = await GetTrackedSeasonAsync(id, ct);

		season.IsActive = true;
		await _db.SaveChangesAsync(ct);

		var list = await EnrichAsync([season], ct);
		return list[0];
	}

	private async Task<Season> GetTrackedSeasonAsync(Guid id, CancellationToken ct)
	{
		var season = await _db.Seasons
			.FirstOrDefaultAsync(x => x.SeasonId == id && !x.IsDeleted, ct);

		if (season is null)
			throw new KeyNotFoundException("Season not found.");

		return season;
	}

	private async Task ClearOtherCurrentAsync(Guid keepSeasonId, CancellationToken ct)
	{
		var others = await _db.Seasons
			.Where(x =>
				!x.IsDeleted &&
				x.IsCurrent &&
				x.SeasonId != keepSeasonId)
			.ToListAsync(ct);

		foreach (var other in others)
			other.IsCurrent = false;
	}

	private async Task<bool> SeasonHasScheduleAsync(Guid seasonId, CancellationToken ct)
	{
		return await _db.Matches
			.AsNoTracking()
			.AnyAsync(x =>
				!x.IsDeleted &&
				x.Tournament.SeasonId == seasonId,
				ct);
	}

	private async Task<List<SeasonDto>> EnrichAsync(
		IReadOnlyList<Season> seasons,
		CancellationToken ct)
	{
		if (seasons.Count == 0)
			return [];

		var ids = seasons.Select(x => x.SeasonId).ToList();

		var registrationCounts = await _db.SeasonRegistrations
			.AsNoTracking()
			.Where(x => ids.Contains(x.SeasonId))
			.GroupBy(x => x.SeasonId)
			.Select(g => new { g.Key, Count = g.Count() })
			.ToDictionaryAsync(x => x.Key, x => x.Count, ct);

		var drafts = await _db.DraftSessions
			.AsNoTracking()
			.Where(x => ids.Contains(x.SeasonId))
			.Select(x => new { x.SeasonId, x.IsActive })
			.ToListAsync(ct);

		var draftBySeason = drafts
			.GroupBy(x => x.SeasonId)
			.ToDictionary(
				g => g.Key,
				g => new
				{
					HasDraft = true,
					DraftActive = g.Any(x => x.IsActive)
				});

		var scheduledIds = await _db.Matches
			.AsNoTracking()
			.Where(x => !x.IsDeleted && ids.Contains(x.Tournament.SeasonId))
			.Select(x => x.Tournament.SeasonId)
			.Distinct()
			.ToListAsync(ct);

		var scheduled = scheduledIds.ToHashSet();

		return seasons.Select(season =>
		{
			registrationCounts.TryGetValue(season.SeasonId, out var registered);
			draftBySeason.TryGetValue(season.SeasonId, out var draft);
			var hasDraft = draft?.HasDraft == true;
			var draftActive = draft?.DraftActive == true;
			var hasSchedule = scheduled.Contains(season.SeasonId);

			return new SeasonDto
			{
				SeasonId = season.SeasonId,
				Name = season.Name,
				Description = season.Description,
				StartDate = season.StartDate,
				EndDate = season.EndDate,
				IsCurrent = season.IsCurrent,
				IsActive = season.IsActive,
				RegistrationOpen = season.RegistrationOpen,
				MaxPlayers = season.MaxPlayers,
				CreatedAtUTC = season.CreatedAtUTC,
				UpdatedAtUTC = season.UpdatedAtUTC,
				TotalWeeks = season.TotalWeeks,
				RegisteredCount = registered,
				HasDraft = hasDraft,
				DraftActive = draftActive,
				HasSchedule = hasSchedule,
				Phase = ResolvePhase(season.IsActive, draftActive, hasDraft, hasSchedule)
			};
		}).ToList();
	}

	private static string ResolvePhase(
		bool isActive,
		bool draftActive,
		bool hasDraft,
		bool hasSchedule)
	{
		if (!isActive)
			return "Closed";

		if (draftActive)
			return "Drafting";

		if (hasSchedule)
			return "Competition";

		if (hasDraft)
			return "DraftReady";

		return "Registration";
	}
}

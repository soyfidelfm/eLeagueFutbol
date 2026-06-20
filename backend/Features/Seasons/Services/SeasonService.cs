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
	private const decimal InitialClubBudget = 250_000_000m;

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

		return seasons.Select(ToDto).ToList();
	}

	public async Task<SeasonDto?> GetByIdAsync(Guid id, CancellationToken ct)
	{
		var season = await _db.Seasons
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.SeasonId == id && !x.IsDeleted, ct);

		return season is null ? null : ToDto(season);
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
		await _db.SaveChangesAsync(ct);

		return ToDto(season);
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

	private static SeasonDto ToDto(Season season)
	{
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
			TotalWeeks = season.TotalWeeks
		};
	}
}
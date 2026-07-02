using backend.Data;
using backend.Entities.GameData;
using backend.Features.Cloudinary.Services;
using backend.Features.GameData.Dtos;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net.Http.Json;

namespace backend.Features.GameData.Services;

public class GameDataService : IGameDataService
{
	private readonly AppDbContext _context;
	private readonly HttpClient _httpClient;
	private readonly ICloudinaryService _cloudinaryService;

	public GameDataService(AppDbContext context, HttpClient httpClient, ICloudinaryService cloudinaryService)
	{
		_context = context;
		_httpClient = httpClient;
		_cloudinaryService = cloudinaryService;
	}

	public async Task<object> ImportAsync(ImportGameClubsDto dto)
	{
		var imported = 0;
		var updated = 0;

		foreach (var item in dto.Teams)
		{
			if (!int.TryParse(item.Teamid, out var externalTeamId))
				continue;

			var league = item.Info.League.FirstOrDefault()?.Leaguename ?? string.Empty;

			var existing = await _context.GameClubs
				.FirstOrDefaultAsync(x => x.ExternalTeamId == externalTeamId);

			if (existing == null)
			{
				_context.GameClubs.Add(new GameClub
				{
					GameClubId = Guid.NewGuid(),
					ExternalTeamId = externalTeamId,
					Name = item.Info.Teamname,
					Slug = item.Slug,
					League = league,
					StadiumName = item.Stadium?.Stadiumname ?? string.Empty,
					OverallRating = item.Info.Overallrating,
					AttackRating = item.Info.Attackrating,
					MidfieldRating = item.Info.Midfieldrating,
					DefenseRating = item.Info.Defenserating,
					IsActive = true
				});

				imported++;
			}
			else
			{
				existing.Name = item.Info.Teamname;
				existing.Slug = item.Slug;
				existing.League = league;
				existing.StadiumName = item.Stadium?.Stadiumname ?? string.Empty;
				existing.OverallRating = item.Info.Overallrating;
				existing.AttackRating = item.Info.Attackrating;
				existing.MidfieldRating = item.Info.Midfieldrating;
				existing.DefenseRating = item.Info.Defenserating;
				existing.IsActive = true;

				updated++;
			}
		}

		await _context.SaveChangesAsync();

		return new
		{
			dto.Total,
			Received = dto.Teams.Count,
			Imported = imported,
			Updated = updated
		};
	}

	public async Task<List<GameClubDto>> GetAllAsync()
	{
		return await _context.GameClubs
			.Where(x => !x.IsDeleted && x.IsActive)
			.OrderBy(x => x.League)
			.ThenBy(x => x.Name)
			.Select(x => new GameClubDto
			{
				GameClubId = x.GameClubId,
				Name = x.Name,
				Code = x.Code,
				League = x.League,
				Country = x.Country,
				LogoUrl = x.LogoUrl ?? ""
			})
			.ToListAsync();
	}

	public async Task<object> SyncFromCmTrackerAsync()
	{
		var page = 0;
		var total = 0;

		_httpClient.DefaultRequestHeaders.Clear();
		_httpClient.DefaultRequestHeaders.Add("Origin", "https://cmtracker.net");
		_httpClient.DefaultRequestHeaders.Add("Referer", "https://cmtracker.net/");
		_httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

		do
		{
			var url = $"https://be.cmtracker.net/api/v2/team?page={page}";

			var dto = await _httpClient.GetFromJsonAsync<ImportGameClubsDto>(url);

			if (dto == null || dto.Teams.Count == 0)
				break;

			total = dto.Total;

			await ImportAsync(dto);

			page++;

		} while (page * 25 < total);

		return new
		{
			Total = total,
			PagesProcessed = page
		};
	}

	public async Task<List<string>> GetLeaguesAsync()
	{
		return await _context.GameClubs
			.Where(x => !x.IsDeleted && x.IsActive)
			.Select(x => x.League)
			.Distinct()
			.OrderBy(x => x)
			.ToListAsync();
	}

	public async Task<List<GameClubDto>> GetByLeagueAsync(string league)
	{
		return await _context.GameClubs
			.Where(x =>
				!x.IsDeleted &&
				x.IsActive &&
				x.League == league)
			.OrderBy(x => x.Name)
			.Select(x => new GameClubDto
			{
				GameClubId = x.GameClubId,
				Name = x.Name,
				Code = x.Code,
				League = x.League,
				Country = x.Country,
				LogoUrl = x.LogoUrl ?? ""
			})
			.ToListAsync();
	}

	public async Task<object> ImportPlayersFromFc26CsvAsync(IFormFile file)
	{
		if (file == null || file.Length == 0)
			throw new InvalidOperationException("CSV file is required.");

		var imported = 0;
		var updated = 0;
		var assignedToFreeAgents = 0;
		var skippedNoClub = 0;
		var skippedNoClubTeamId = 0;

		using var stream = file.OpenReadStream();
		using var reader = new StreamReader(stream);
		using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

		var rows = csv.GetRecords<Fc26PlayerCsvRow>().ToList();

		var clubsByExternalTeamId = await _context.GameClubs
			.AsNoTracking()
			.Where(x => !x.IsDeleted && x.IsActive)
			.ToDictionaryAsync(x => x.ExternalTeamId);

		var freeAgentClub = await _context.GameClubs
			.AsNoTracking()
			.FirstAsync(x =>
				x.ExternalTeamId == -1 &&
				!x.IsDeleted &&
				x.IsActive);

		var existingPlayersBySofifaId = await _context.GamePlayers
			.Where(x => !x.IsDeleted)
			.ToDictionaryAsync(x => x.SofifaPlayerId);

		foreach (var row in rows)
		{
			GameClub club;

			if (!row.ClubTeamId.HasValue || row.ClubTeamId.Value <= 0)
			{
				skippedNoClubTeamId++;
				assignedToFreeAgents++;
				club = freeAgentClub;
			}
			else if (!clubsByExternalTeamId.TryGetValue(row.ClubTeamId.Value, out club!))
			{
				skippedNoClub++;
				assignedToFreeAgents++;
				club = freeAgentClub;
			}

			var clubTeamId = row.ClubTeamId.HasValue && row.ClubTeamId.Value > 0
				? row.ClubTeamId
				: -1;

			var clubName = string.IsNullOrWhiteSpace(row.ClubName)
				? "Free Agents"
				: row.ClubName.Trim();

			if (!existingPlayersBySofifaId.TryGetValue(row.SofifaPlayerId, out var existing))
			{
				var player = new GamePlayer
				{
					GamePlayerId = Guid.NewGuid(),

					SofifaPlayerId = row.SofifaPlayerId,
					FutbinId = null,

					Name = row.Name.Trim(),
					FullName = row.FullName.Trim(),

					Overall = row.Overall,
					Potential = row.Potential,

					Position = row.Positions.Trim(),

					ValueEur = row.ValueEur,
					WageEur = row.WageEur,
					ReleaseClauseEur = row.ReleaseClauseEur,

					Age = row.Age,
					DateOfBirth = row.DateOfBirth.HasValue
						? DateTime.SpecifyKind(row.DateOfBirth.Value, DateTimeKind.Utc)
						: null,

					HeightCm = row.HeightCm,
					WeightKg = row.WeightKg,

					ClubTeamId = clubTeamId,
					ClubName = clubName,
					ClubJerseyNumber = row.ClubJerseyNumber,
					ClubContractValidUntilYear = row.ClubContractValidUntilYear,

					NationalityId = row.NationalityId,
					Nation = row.Nation.Trim(),
					NationTeamId = row.NationTeamId,

					PreferredFoot = row.PreferredFoot.Trim(),
					WeakFoot = row.WeakFoot,
					SkillMoves = row.SkillMoves,

					Pace = row.Pace,
					Shooting = row.Shooting,
					Passing = row.Passing,
					Dribbling = row.Dribbling,
					Defending = row.Defending,
					Physic = row.Physic,

					ImageUrl = row.ImageUrl,

					GameClubId = club.GameClubId,
					IsActive = true
				};

				_context.GamePlayers.Add(player);
				existingPlayersBySofifaId[row.SofifaPlayerId] = player;

				imported++;
			}
			else
			{
				existing.Name = row.Name.Trim();
				existing.FullName = row.FullName.Trim();
				existing.Overall = row.Overall;
				existing.Potential = row.Potential;
				existing.Position = row.Positions.Trim();

				existing.ValueEur = row.ValueEur;
				existing.WageEur = row.WageEur;
				existing.ReleaseClauseEur = row.ReleaseClauseEur;

				existing.Age = row.Age;
				existing.DateOfBirth = row.DateOfBirth.HasValue
					? DateTime.SpecifyKind(row.DateOfBirth.Value, DateTimeKind.Utc)
					: null;

				existing.HeightCm = row.HeightCm;
				existing.WeightKg = row.WeightKg;

				existing.ClubTeamId = clubTeamId;
				existing.ClubName = clubName;
				existing.ClubJerseyNumber = row.ClubJerseyNumber;
				existing.ClubContractValidUntilYear = row.ClubContractValidUntilYear;

				existing.NationalityId = row.NationalityId;
				existing.Nation = row.Nation.Trim();
				existing.NationTeamId = row.NationTeamId;

				existing.PreferredFoot = row.PreferredFoot.Trim();
				existing.WeakFoot = row.WeakFoot;
				existing.SkillMoves = row.SkillMoves;

				existing.Pace = row.Pace;
				existing.Shooting = row.Shooting;
				existing.Passing = row.Passing;
				existing.Dribbling = row.Dribbling;
				existing.Defending = row.Defending;
				existing.Physic = row.Physic;

				existing.ImageUrl = row.ImageUrl;
				existing.GameClubId = club.GameClubId;
				existing.IsActive = true;

				updated++;
			}
		}

		await _context.SaveChangesAsync();

		return new
		{
			TotalRows = rows.Count,
			Imported = imported,
			Updated = updated,
			AssignedToFreeAgents = assignedToFreeAgents,
			SkippedNoClubTeamId = skippedNoClubTeamId,
			SkippedNoClubMatch = skippedNoClub
		};
	}

	public async Task<List<GamePlayerDto>> GetPlayersAsync(string? search)
	{
		var query = _context.GamePlayers
			.AsNoTracking()
			.Include(x => x.GameClub)
			.Where(x => !x.IsDeleted && x.IsActive)
			.AsQueryable();

		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = search.Trim().ToLower();

			query = query.Where(x =>
				x.Name.ToLower().Contains(term) ||
				x.FullName.ToLower().Contains(term) ||
				x.Position.ToLower().Contains(term) ||
				x.Nation.ToLower().Contains(term) ||
				x.GameClub.Name.ToLower().Contains(term));
		}

		return await query
			.OrderByDescending(x => x.Overall)
			.ThenBy(x => x.Name)
			.Select(x => new GamePlayerDto
			{
				GamePlayerId = x.GamePlayerId,

				SofifaPlayerId = x.SofifaPlayerId,
				FutbinId = x.FutbinId,

				Name = x.Name,
				FullName = x.FullName,

				Overall = x.Overall,
				Potential = x.Potential,

				Position = x.Position,
				Nation = x.Nation,

				Age = x.Age,
				HeightCm = x.HeightCm,
				WeightKg = x.WeightKg,

				PreferredFoot = x.PreferredFoot,

				ImageUrl = x.ImageUrl,

				GameClubId = x.GameClubId,
				GameClubName = x.GameClub.Name
			})
			.ToListAsync();
	}

	private async Task<Guid> GetDefaultGameClubIdAsync()
	{
		var club = await _context.GameClubs
			.Where(x => !x.IsDeleted && x.IsActive)
			.OrderBy(x => x.Name)
			.FirstOrDefaultAsync();

		if (club == null)
			throw new InvalidOperationException("No GameClub found. Sync clubs before importing players.");

		return club.GameClubId;
	}
	public async Task<GameClubLogoDto?> UploadLogoAsync(Guid gameClubId, IFormFile file)
	{
		if (file is null || file.Length == 0)
			throw new ArgumentException("No file uploaded.");

		var club = await _context.GameClubs
			.FirstOrDefaultAsync(x =>
				x.GameClubId == gameClubId &&
				!x.IsDeleted);

		if (club is null)
			return null;

		var upload = await _cloudinaryService.UploadImageAsync(
			file,
			"eleaguefutbol/clubs"
		);

		club.LogoUrl = upload.Url;
		club.LogoPublicId = upload.PublicId;
		club.UpdatedAtUTC = DateTime.UtcNow;

		await _context.SaveChangesAsync();

		return new GameClubLogoDto
		{
			GameClubId = club.GameClubId,
			Name = club.Name,
			LogoUrl = club.LogoUrl!,
			LogoPublicId = club.LogoPublicId!
		};
	}
	public async Task<GameClubDto?> GetByIdAsync(Guid gameClubId)
	{
		return await _context.GameClubs
			.AsNoTracking()
			.Where(x =>
				x.GameClubId == gameClubId &&
				!x.IsDeleted &&
				x.IsActive)
			.Select(x => new GameClubDto
			{
				GameClubId = x.GameClubId,
				Name = x.Name,
				Code = x.Code,
				League = x.League,
				Country = x.Country,
				LogoUrl = x.LogoUrl ?? ""
			})
			.FirstOrDefaultAsync();
	}
}
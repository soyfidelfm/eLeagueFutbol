using backend.Data;
using backend.Entities.Teams;
using backend.Features.GameClubs.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace backend.Features.GameClubs.Services;

public class GameClubService : IGameClubService
{
	private readonly AppDbContext _context;
	private readonly HttpClient _httpClient;

	public GameClubService(
	AppDbContext context,
	HttpClient httpClient)
	{
		_context = context;
		_httpClient = httpClient;
	}

	public async Task<object> ImportAsync(ImportGameClubsDto dto)
	{
		var imported = 0;
		var updated = 0;

		foreach (var item in dto.Teams)
		{
			if (!int.TryParse(item.Teamid, out var externalTeamId))
				continue;

			var league =
				item.Info.League.FirstOrDefault()?.Leaguename
				?? string.Empty;

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
				LogoUrl = x.LogoUrl
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
			var url =
				$"https://be.cmtracker.net/api/v2/team?page={page}";

			var dto =
				await _httpClient.GetFromJsonAsync<ImportGameClubsDto>(url);

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
				LogoUrl = x.LogoUrl
			})
			.ToListAsync();
	}
}
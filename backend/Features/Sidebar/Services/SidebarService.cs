using backend.Data;
using backend.Features.Sidebar.Dtos;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.Sidebar.Services;

public class SidebarService : ISidebarService
{
	private readonly AppDbContext _context;

	public SidebarService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<SidebarDto> GetAsync()
	{
		var nowUtc = DateTime.UtcNow;

		var currentSeason = await _context.Seasons
			.AsNoTracking()
			.Where(x =>
				!x.IsDeleted &&
				x.IsActive &&
				x.IsCurrent)
			.OrderByDescending(x => x.StartDate)
			.FirstOrDefaultAsync();

		SidebarSeasonDto? seasonDto = null;

		if (currentSeason is not null)
		{
			var totalClubs = await _context.SeasonRegistrations
				.AsNoTracking()
				.CountAsync(x =>					
					x.SeasonId == currentSeason.SeasonId);

			var totalMatches =	totalClubs * (totalClubs - 1);

			seasonDto = new SidebarSeasonDto
			{
				Label = "CURRENT SEASON",
				Name = currentSeason.Name,
				TotalClubs = totalClubs,
				TotalMatches = totalMatches
			};
		}

		var nextCup = await _context.Tournaments
			.AsNoTracking()
			.Where(x =>
				!x.IsDeleted &&
				x.StartDate > nowUtc)
			.OrderBy(x => x.StartDate)
			.FirstOrDefaultAsync();

		SidebarCupDto? cupDto = null;

		if (nextCup is not null)
		{
			cupDto = new SidebarCupDto
			{
				Name = nextCup.Name,
				StartsAtUtc = nextCup.StartDate
			};
		}

		return new SidebarDto
		{
			Season = seasonDto,
			Cup = cupDto
		};
	}
}
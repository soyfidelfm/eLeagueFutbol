using backend.Data;
using backend.Features.GameClubs.Dtos;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.GameClubs.Services;

public class GameClubService : IGameClubService
{
	private readonly AppDbContext _context;

	public GameClubService(AppDbContext context)
	{
		_context = context;
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
}
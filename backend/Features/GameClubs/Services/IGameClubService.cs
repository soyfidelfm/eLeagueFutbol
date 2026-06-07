using backend.Features.GameClubs.Dtos;

namespace backend.Features.GameClubs.Services;

public interface IGameClubService
{
	Task<List<GameClubDto>> GetAllAsync();
	Task<object> ImportAsync(ImportGameClubsDto dto);
	Task<object> SyncFromCmTrackerAsync();
	Task<List<string>> GetLeaguesAsync();

	Task<List<GameClubDto>> GetByLeagueAsync(string league);
}
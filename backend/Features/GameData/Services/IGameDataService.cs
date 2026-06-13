using backend.Features.GameData.Dtos;

namespace backend.Features.GameData.Services;

public interface IGameDataService
{
	Task<object> ImportAsync(ImportGameClubsDto dto);

	Task<List<GameClubDto>> GetAllAsync();

	Task<object> SyncFromCmTrackerAsync();

	Task<List<string>> GetLeaguesAsync();

	Task<List<GameClubDto>> GetByLeagueAsync(string league);

	Task<object> ImportPlayersFromFc26CsvAsync(IFormFile file);

	Task<List<GamePlayerDto>> GetPlayersAsync(string? search);
	Task<GameClubLogoDto?> UploadLogoAsync(Guid gameClubId, IFormFile file);
	Task<GameClubDto?> GetByIdAsync(Guid gameClubId);
}
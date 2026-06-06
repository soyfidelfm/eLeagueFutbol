using backend.Features.GameClubs.Dtos;

namespace backend.Features.GameClubs.Services;

public interface IGameClubService
{
	Task<List<GameClubDto>> GetAllAsync();
}
namespace backend.Features.Teams.Services;

using backend.Features.Teams.Dtos;

public interface ITeamService
{
	Task<List<TeamDto>> GetAllAsync(string auth0Id);
	Task<TeamDto?> GetMineAsync(string auth0Id);
	Task<TeamDto> CreateAsync(string auth0Id, CreateTeamDto dto);
	Task<TeamDto> AddMemberAsync(string auth0Id, Guid teamId, AddTeamMemberDto dto);
	Task LeaveOrRemoveAsync(string auth0Id, Guid teamId, Guid userId);
}

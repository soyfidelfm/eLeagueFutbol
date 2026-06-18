using backend.Features.Users.Dtos;
using System.Security.Claims;

namespace backend.Features.Users.Services;

public interface IUserService
{
	Task<List<UserDto>> GetAllAsync();

	Task<UserDto?> GetByIdAsync(Guid userId);

	Task<UserDto?> SyncAsync(SyncUserDto dto, ClaimsPrincipal principal);

	Task<UserDto?> UpdateAsync(Guid userId, UpdateUserDto dto);
	Task<MeUserDto?> GetMeAsync(ClaimsPrincipal principal);
	Task<List<UserDto>> SearchAsync(string term, ClaimsPrincipal principal);
}
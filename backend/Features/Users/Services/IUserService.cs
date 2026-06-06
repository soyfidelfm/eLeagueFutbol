using backend.Features.Users.Dtos;

namespace backend.Features.Users.Services;

public interface IUserService
{
	Task<List<UserDto>> GetAllAsync();

	Task<UserDto?> GetByIdAsync(Guid userId);

	Task<UserDto> SyncAsync(SyncUserDto dto);

	Task<UserDto?> UpdateAsync(Guid userId, UpdateUserDto dto);
}
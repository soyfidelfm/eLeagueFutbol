using backend.Data;
using backend.Entities.Identity;
using backend.Features.Users.Dtos;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.Users.Services;

public class UserService : IUserService
{
	private readonly AppDbContext _context;

	public UserService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<List<UserDto>> GetAllAsync()
	{
		return await _context.Users
			.Where(x => !x.IsDeleted)
			.OrderBy(x => x.DisplayName)
			.Select(x => new UserDto
			{
				UserId = x.UserId,
				Auth0Id = x.Auth0Id,
				Email = x.Email,
				DisplayName = x.DisplayName,
				AvatarUrl = x.AvatarUrl,
				TimeZone = x.TimeZone,
				LastLoginAtUTC = x.LastLoginAtUTC,
				IsActive = x.IsActive
			})
			.ToListAsync();
	}

	public async Task<UserDto?> GetByIdAsync(Guid userId)
	{
		return await _context.Users
			.Where(x => x.UserId == userId && !x.IsDeleted)
			.Select(x => new UserDto
			{
				UserId = x.UserId,
				Auth0Id = x.Auth0Id,
				Email = x.Email,
				DisplayName = x.DisplayName,
				AvatarUrl = x.AvatarUrl,
				TimeZone = x.TimeZone,
				LastLoginAtUTC = x.LastLoginAtUTC,
				IsActive = x.IsActive
			})
			.FirstOrDefaultAsync();
	}

	public async Task<UserDto> SyncAsync(SyncUserDto dto)
	{
		var timeZone = string.IsNullOrWhiteSpace(dto.TimeZone)
			? "UTC"
			: dto.TimeZone;

		var user = await _context.Users
			.FirstOrDefaultAsync(x => x.Auth0Id == dto.Auth0Id);

		if (user == null)
		{
			user = new User
			{
				UserId = Guid.NewGuid(),
				Auth0Id = dto.Auth0Id,
				Email = dto.Email,
				DisplayName = dto.DisplayName,
				AvatarUrl = dto.AvatarUrl,
				TimeZone = timeZone,
				LastLoginAtUTC = DateTime.UtcNow,
				IsActive = true
			};

			_context.Users.Add(user);
		}
		else
		{
			user.Email = dto.Email;
			user.DisplayName = dto.DisplayName;
			user.AvatarUrl = dto.AvatarUrl;
			user.TimeZone = timeZone;
			user.LastLoginAtUTC = DateTime.UtcNow;
			user.IsActive = true;
		}

		await _context.SaveChangesAsync();

		return new UserDto
		{
			UserId = user.UserId,
			Auth0Id = user.Auth0Id,
			Email = user.Email,
			DisplayName = user.DisplayName,
			AvatarUrl = user.AvatarUrl,
			TimeZone = user.TimeZone,
			LastLoginAtUTC = user.LastLoginAtUTC,
			IsActive = user.IsActive
		};
	}

	public async Task<UserDto?> UpdateAsync(Guid userId, UpdateUserDto dto)
	{
		var user = await _context.Users
			.FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

		if (user == null)
			return null;

		user.DisplayName = dto.DisplayName;
		user.AvatarUrl = dto.AvatarUrl;
		user.TimeZone = string.IsNullOrWhiteSpace(dto.TimeZone)
			? "UTC"
			: dto.TimeZone;

		await _context.SaveChangesAsync();

		return new UserDto
		{
			UserId = user.UserId,
			Auth0Id = user.Auth0Id,
			Email = user.Email,
			DisplayName = user.DisplayName,
			AvatarUrl = user.AvatarUrl,
			TimeZone = user.TimeZone,
			LastLoginAtUTC = user.LastLoginAtUTC,
			IsActive = user.IsActive
		};
	}
}
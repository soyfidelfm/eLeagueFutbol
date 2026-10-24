using backend.Data;
using backend.Entities.Identity;
using backend.Features.Users.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
				IsActive = x.IsActive,
				ClubName = x.ClubName,
				GameClubId = x.GameClubId,
				GamerTag = x.GamerTag,
				Role = x.Role
			})
			.ToListAsync();
	}

	public async Task<UserDto?> GetByIdAsync(Guid userId)
	{
		var user = await _context.Users
			.Where(x => x.UserId == userId && !x.IsDeleted)
			.Select(x => new UserDto
			{
				UserId = x.UserId,
				Auth0Id = x.Auth0Id,
				Email = x.Email,
				DisplayName = x.DisplayName,
				AvatarUrl = x.AvatarUrl,
				TimeZone = x.TimeZone,
				ClubName = x.ClubName,
				GameClubId = x.GameClubId,
				LastLoginAtUTC = x.LastLoginAtUTC,
				IsActive = x.IsActive,
				GamerTag = x.GamerTag,
				Role = x.Role
			})
			.FirstOrDefaultAsync();

		if (user?.GameClubId != null)
		{
			user.GameClubLeague = await _context.GameClubs
				.Where(x => x.GameClubId == user.GameClubId)
				.Select(x => x.League)
				.FirstOrDefaultAsync() ?? string.Empty;
		}

		return user;
	}

	public async Task<UserDto?> SyncAsync(SyncUserDto dto, ClaimsPrincipal principal)
	{
		var auth0Id =
			principal.FindFirstValue(ClaimTypes.NameIdentifier)
			?? principal.FindFirstValue("sub");

		if (string.IsNullOrWhiteSpace(auth0Id))
			return null;

		var timeZone = string.IsNullOrWhiteSpace(dto.TimeZone)
			? "UTC"
			: dto.TimeZone;

		var user = await _context.Users
			.FirstOrDefaultAsync(x => x.Auth0Id == auth0Id && !x.IsDeleted);

		if (user == null)
		{
			user = new User
			{
				UserId = Guid.NewGuid(),
				Auth0Id = auth0Id,
				Email = dto.Email,
				DisplayName = dto.DisplayName,
				AvatarUrl = dto.AvatarUrl,
				TimeZone = timeZone,
				LastLoginAtUTC = DateTime.UtcNow,
				IsActive = true,
				ClubName = string.Empty,
				GamerTag = string.Empty,
				GameClubId = null,
				Role = "Player"
			};

			_context.Users.Add(user);
		}
		else
		{
			user.Email = dto.Email;
			user.DisplayName = dto.DisplayName;
			user.TimeZone = timeZone;
			user.LastLoginAtUTC = DateTime.UtcNow;
			user.IsActive = true;

			if (string.IsNullOrWhiteSpace(user.AvatarUrl))
				user.AvatarUrl = dto.AvatarUrl;

			if (string.IsNullOrWhiteSpace(user.Role))
				user.Role = "Player";
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
			IsActive = user.IsActive,
			ClubName = user.ClubName,
			GameClubId = user.GameClubId,
			GamerTag = user.GamerTag,
			Role = user.Role,
			GameClubLeague = string.Empty
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

		user.ClubName = dto.ClubName;
		user.GameClubId = dto.GameClubId;
		user.GamerTag = dto.GamerTag;

		await _context.SaveChangesAsync();

		var gameClubLeague = user.GameClubId == null
			? string.Empty
			: await _context.GameClubs
				.Where(x => x.GameClubId == user.GameClubId)
				.Select(x => x.League)
				.FirstOrDefaultAsync() ?? string.Empty;

		return new UserDto
		{
			UserId = user.UserId,
			Auth0Id = user.Auth0Id,
			Email = user.Email,
			DisplayName = user.DisplayName,
			AvatarUrl = user.AvatarUrl,
			TimeZone = user.TimeZone,
			LastLoginAtUTC = user.LastLoginAtUTC,
			IsActive = user.IsActive,
			ClubName = user.ClubName,
			GameClubId = user.GameClubId,
			GamerTag = user.GamerTag,
			Role = user.Role,
			GameClubLeague = gameClubLeague
		};
	}

	public async Task<MeUserDto?> GetMeAsync(ClaimsPrincipal principal)
	{
		var auth0Id =
			principal.FindFirstValue(ClaimTypes.NameIdentifier)
			?? principal.FindFirstValue("sub");

		if (string.IsNullOrWhiteSpace(auth0Id))
			return null;

		var user = await _context.Users
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted);

		if (user == null)
		{
			var email =
				principal.FindFirstValue(ClaimTypes.Email)
				?? principal.FindFirstValue("email")
				?? string.Empty;

			var displayName =
				principal.FindFirstValue(ClaimTypes.Name)
				?? principal.FindFirstValue("name")
				?? principal.FindFirstValue("nickname")
				?? email;

			var avatarUrl =
				principal.FindFirstValue("picture")
				?? string.Empty;

			user = new User
			{
				UserId = Guid.NewGuid(),
				Auth0Id = auth0Id,
				Email = email,
				DisplayName = displayName,
				AvatarUrl = avatarUrl,
				TimeZone = "America/Los_Angeles",
				ClubName = string.Empty,
				GamerTag = string.Empty,
				IsActive = true,
				LastLoginAtUTC = DateTime.UtcNow,
				Role = "Player"
			};

			_context.Users.Add(user);
			await _context.SaveChangesAsync();
		}
		else
		{
			user.LastLoginAtUTC = DateTime.UtcNow;
			await _context.SaveChangesAsync();
		}

		var gameClubLeague = user.GameClubId == null
			? string.Empty
			: await _context.GameClubs
				.AsNoTracking()
				.Where(x => x.GameClubId == user.GameClubId)
				.Select(x => x.League)
				.FirstOrDefaultAsync() ?? string.Empty;

		return new MeUserDto
		{
			UserId = user.UserId,
			Auth0Id = user.Auth0Id,
			Email = user.Email,
			DisplayName = user.DisplayName,
			AvatarUrl = user.AvatarUrl,
			TimeZone = user.TimeZone,
			ClubName = user.ClubName,
			GameClubId = user.GameClubId,
			GameClubLeague = gameClubLeague,
			GamerTag = user.GamerTag,
			IsActive = user.IsActive,
			LastLoginAtUTC = user.LastLoginAtUTC,
			Role = user.Role,

			Claims = principal.Claims
				.Select(c => new ClaimDto
				{
					Type = c.Type,
					Value = c.Value
				})
				.ToList()
		};
	}

	public async Task<List<UserDto>> SearchAsync(string term, ClaimsPrincipal principal)
	{
		var auth0Id =
			principal.FindFirstValue(ClaimTypes.NameIdentifier)
			?? principal.FindFirstValue("sub");

		if (string.IsNullOrWhiteSpace(auth0Id))
			return [];

		term = term.Trim().ToLower();

		if (term.Length < 2)
			return [];

		var me = await _context.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(x =>
				x.Auth0Id == auth0Id &&
				!x.IsDeleted);

		if (me is null)
			return [];

		return await _context.Users
			.AsNoTracking()
			.Where(x =>
				!x.IsDeleted &&
				x.UserId != me.UserId &&
				(
					x.DisplayName.ToLower().Contains(term) ||
					x.Email.ToLower().Contains(term) ||
					x.GamerTag.ToLower().Contains(term) ||
					x.ClubName.ToLower().Contains(term)
				))
			.OrderBy(x => x.DisplayName)
			.Take(20)
			.Select(x => new UserDto
			{
				UserId = x.UserId,
				Auth0Id = x.Auth0Id,
				Email = x.Email,
				DisplayName = x.DisplayName,
				AvatarUrl = x.AvatarUrl,
				TimeZone = x.TimeZone,
				LastLoginAtUTC = x.LastLoginAtUTC,
				IsActive = x.IsActive,
				ClubName = x.ClubName,
				GameClubId = x.GameClubId,
				GamerTag = x.GamerTag,
				Role = x.Role
			})
			.ToListAsync();
	}

	public static readonly string[] SupportedLanguages = ["en", "es"];

	public async Task<UserSettingsDto?> GetSettingsAsync(ClaimsPrincipal principal)
	{
		var user = await FindCurrentUserAsync(principal);

		return user is null ? null : ToSettingsDto(user);
	}

	public async Task<UserSettingsDto?> UpdateSettingsAsync(ClaimsPrincipal principal, UserSettingsDto dto)
	{
		var user = await FindCurrentUserAsync(principal);

		if (user is null)
			return null;

		var timeZone = (dto.TimeZone ?? string.Empty).Trim();

		if (string.IsNullOrWhiteSpace(timeZone) ||
			!TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out _))
			throw new ArgumentException("Invalid time zone.");

		var language = (dto.Language ?? string.Empty).Trim().ToLowerInvariant();

		if (!SupportedLanguages.Contains(language))
			throw new ArgumentException("Invalid language. Supported: en, es.");

		user.TimeZone = timeZone;
		user.Language = language;
		user.PushNotificationsEnabled = dto.PushNotificationsEnabled;
		user.InAppToastsEnabled = dto.InAppToastsEnabled;
		user.UpdatedAtUTC = DateTime.UtcNow;
		user.UpdatedByUserId = user.UserId;

		await _context.SaveChangesAsync();

		return ToSettingsDto(user);
	}

	private async Task<User?> FindCurrentUserAsync(ClaimsPrincipal principal)
	{
		var auth0Id =
			principal.FindFirstValue(ClaimTypes.NameIdentifier)
			?? principal.FindFirstValue("sub");

		if (string.IsNullOrWhiteSpace(auth0Id))
			return null;

		return await _context.Users.FirstOrDefaultAsync(x =>
			x.Auth0Id == auth0Id &&
			!x.IsDeleted);
	}

	private static UserSettingsDto ToSettingsDto(User user) => new()
	{
		TimeZone = string.IsNullOrWhiteSpace(user.TimeZone) ? "UTC" : user.TimeZone,
		Language = SupportedLanguages.Contains(user.Language) ? user.Language : "en",
		PushNotificationsEnabled = user.PushNotificationsEnabled,
		InAppToastsEnabled = user.InAppToastsEnabled
	};
}

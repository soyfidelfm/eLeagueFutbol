using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.Users.Services;

public class AdminAccessService : IAdminAccessService
{
	private readonly AppDbContext _db;

	public AdminAccessService(AppDbContext db)
	{
		_db = db;
	}

	public async Task EnsureAdminAsync(string auth0Id, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(auth0Id))
			throw new UnauthorizedAccessException("User token is missing Auth0 id.");

		var role = await _db.Users
			.AsNoTracking()
			.Where(x =>
				x.Auth0Id == auth0Id &&
				x.IsActive &&
				!x.IsDeleted)
			.Select(x => x.Role)
			.FirstOrDefaultAsync(ct);

		if (string.IsNullOrWhiteSpace(role))
			throw new UnauthorizedAccessException("User not found.");

		if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
			throw new UnauthorizedAccessException("Only admins can perform this action.");
	}
}

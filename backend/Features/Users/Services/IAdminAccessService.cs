namespace backend.Features.Users.Services;

public interface IAdminAccessService
{
	Task EnsureAdminAsync(string auth0Id, CancellationToken ct = default);
}

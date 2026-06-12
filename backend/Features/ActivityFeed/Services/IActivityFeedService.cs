namespace backend.Features.ActivityFeed.Services
{
	public interface IActivityFeedService
	{
		Task AddAsync(Guid userId, string type, string text);
	}
}

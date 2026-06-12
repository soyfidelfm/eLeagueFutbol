using backend.Data;
using backend.Entities.Dashboard;

namespace backend.Features.ActivityFeed.Services
{
	public class ActivityFeedService : IActivityFeedService
	{
		private readonly AppDbContext _context;

		public ActivityFeedService(AppDbContext context)
		{
			_context = context;
		}

		public async Task AddAsync(Guid userId, string type, string text)
		{
			var activity = new Entities.Dashboard.ActivityFeed
			{
				Id = Guid.NewGuid(),
				UserId = userId,
				Type = type,
				Text = text,
				CreatedAtUTC = DateTime.UtcNow
			};

			_context.ActivityFeeds.Add(activity);

			await _context.SaveChangesAsync();
		}
	}
}

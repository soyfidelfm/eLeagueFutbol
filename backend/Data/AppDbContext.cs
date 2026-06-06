namespace backend.Data;

using backend.Entities.Common;
using backend.Entities.Dashboard;
using backend.Entities.Identity;
using backend.Entities.Stats;
using backend.Entities.Teams;
using backend.Entities.Tournaments;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options)
		: base(options)
	{
	}

	public DbSet<User> Users => Set<User>();
	public DbSet<Team> Teams => Set<Team>();
	public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
	public DbSet<Season> Seasons => Set<Season>();
	public DbSet<Tournament> Tournaments => Set<Tournament>();
	public DbSet<Match> Matches => Set<Match>();
	public DbSet<MatchPlayerStats> MatchPlayerStats => Set<MatchPlayerStats>();

	public DbSet<PlayerRanking> PlayerRankings => Set<PlayerRanking>();
	public DbSet<ActivityFeed> ActivityFeeds => Set<ActivityFeed>();
	public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
	public DbSet<GameClub> GameClubs { get; set; }
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<PlayerRanking>(entity =>
		{
			entity.HasKey(x => x.Id);

			entity.HasOne(x => x.User)
				.WithMany()
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.Property(x => x.Form)
				.HasMaxLength(50);
		});

		modelBuilder.Entity<ActivityFeed>(entity =>
		{
			entity.HasKey(x => x.Id);

			entity.HasOne(x => x.User)
				.WithMany()
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.Property(x => x.Text)
				.HasMaxLength(300);
		});

		modelBuilder.Entity<NewsArticle>(entity =>
		{
			entity.HasKey(x => x.Id);

			entity.Property(x => x.Title)
				.HasMaxLength(200);

			entity.Property(x => x.Content)
				.HasMaxLength(2000);
		});

		modelBuilder.Entity<Match>()
			.HasKey(m => m.MatchId);

		modelBuilder.Entity<Match>()
			.HasOne(m => m.HomeTeam)
			.WithMany()
			.HasForeignKey(m => m.HomeTeamId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<Match>()
			.HasOne(m => m.AwayTeam)
			.WithMany()
			.HasForeignKey(m => m.AwayTeamId)
			.OnDelete(DeleteBehavior.Restrict);
	}

	public override Task<int> SaveChangesAsync(
		CancellationToken cancellationToken = default)
	{
		var entries = ChangeTracker.Entries<BaseEntity>();

		foreach (var entry in entries)
		{
			if (entry.State == EntityState.Added)
			{
				entry.Entity.CreatedAtUTC = DateTime.UtcNow;
				entry.Entity.IsDeleted = false;
			}

			if (entry.State == EntityState.Modified)
			{
				entry.Entity.UpdatedAtUTC = DateTime.UtcNow;
			}
		}

		return base.SaveChangesAsync(cancellationToken);
	}
}
namespace backend.Data;

using backend.Entities.Common;
using backend.Entities.Dashboard;
using backend.Entities.Draft;
using backend.Entities.GameData;
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

	public DbSet<SeasonRegistration> SeasonRegistrations => Set<SeasonRegistration>();

	public DbSet<GameClub> GameClubs => Set<GameClub>();
	public DbSet<GamePlayer> GamePlayers => Set<GamePlayer>();

	public DbSet<ClubLineup> ClubLineups => Set<ClubLineup>();
	public DbSet<ClubLineupPlayer> ClubLineupPlayers => Set<ClubLineupPlayer>();
	public DbSet<DraftSession> DraftSessions => Set<DraftSession>();
	public DbSet<DraftOrder> DraftOrders => Set<DraftOrder>();
	public DbSet<DraftPick> DraftPicks => Set<DraftPick>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<User>()
			.HasOne(x => x.GameClub)
			.WithOne(x => x.User)
			.HasForeignKey<User>(x => x.GameClubId)
			.OnDelete(DeleteBehavior.SetNull);

		modelBuilder.Entity<SeasonRegistration>()
			.HasIndex(x => new { x.SeasonId, x.UserId })
			.IsUnique();

		modelBuilder.Entity<SeasonRegistration>()
			.HasOne(x => x.Season)
			.WithMany(x => x.Registrations)
			.HasForeignKey(x => x.SeasonId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<SeasonRegistration>()
			.HasOne(x => x.User)
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<SeasonRegistration>()
			.HasOne(x => x.GameClub)
			.WithMany()
			.HasForeignKey(x => x.GameClubId)
			.OnDelete(DeleteBehavior.Restrict);

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

		modelBuilder.Entity<Match>(entity =>
		{
			entity.HasKey(x => x.MatchId);

			entity.HasOne(x => x.Tournament)
				.WithMany(x => x.Matches)
				.HasForeignKey(x => x.TournamentId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(x => x.HomeUser)
				.WithMany(x => x.HomeMatches)
				.HasForeignKey(x => x.HomeUserId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(x => x.AwayUser)
				.WithMany(x => x.AwayMatches)
				.HasForeignKey(x => x.AwayUserId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(x => x.HomeGameClub)
				.WithMany()
				.HasForeignKey(x => x.HomeGameClubId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(x => x.AwayGameClub)
				.WithMany()
				.HasForeignKey(x => x.AwayGameClubId)
				.OnDelete(DeleteBehavior.Restrict);
		});
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
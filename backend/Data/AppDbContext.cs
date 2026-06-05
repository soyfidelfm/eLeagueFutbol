namespace backend.Data;

using backend.Entities.Common;
using backend.Entities.Identity;
using backend.Entities.Teams;
using backend.Entities.Tournaments;
using backend.Entities.Stats;
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

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// Match -> HomeTeam
		modelBuilder.Entity<Match>()
			.HasOne(m => m.HomeTeam)
			.WithMany()
			.HasForeignKey(m => m.HomeTeamId)
			.OnDelete(DeleteBehavior.Restrict);

		// Match -> AwayTeam
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
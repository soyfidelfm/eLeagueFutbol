namespace backend.Data;

using backend.Entities.Clubs;
using backend.Entities.Common;
using backend.Entities.Dashboard;
using backend.Entities.Draft;
using backend.Entities.GameData;
using backend.Entities.Identity;
using backend.Entities.Messaging;
using backend.Entities.Notifications;
using backend.Entities.Stats;
using backend.Entities.Teams;
using backend.Entities.Tournaments;
using backend.Entities.Transfers;
using backend.Entities.MatchSummary;
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

	public DbSet<PlayerRanking> PlayerRankings => Set<PlayerRanking>();
	public DbSet<ActivityFeed> ActivityFeeds => Set<ActivityFeed>();
	public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();

	public DbSet<SeasonRegistration> SeasonRegistrations => Set<SeasonRegistration>();

	public DbSet<GameClub> GameClubs => Set<GameClub>();
	public DbSet<GamePlayer> GamePlayers => Set<GamePlayer>();

	public DbSet<UserClub> UserClubs => Set<UserClub>();
	public DbSet<UserClubPlayer> UserClubPlayers => Set<UserClubPlayer>();

	public DbSet<ClubLineup> ClubLineups => Set<ClubLineup>();
	public DbSet<ClubLineupPlayer> ClubLineupPlayers => Set<ClubLineupPlayer>();

	public DbSet<TransferRecord> TransferRecords => Set<TransferRecord>();
	public DbSet<UserClubBudgetTransaction> UserClubBudgetTransactions => Set<UserClubBudgetTransaction>();

	public DbSet<DraftSession> DraftSessions => Set<DraftSession>();
	public DbSet<DraftOrder> DraftOrders => Set<DraftOrder>();
	public DbSet<DraftPick> DraftPicks => Set<DraftPick>();

	public DbSet<NotificationToken> NotificationTokens => Set<NotificationToken>();
	public DbSet<Message> Messages => Set<Message>();
	public DbSet<Notification> Notifications => Set<Notification>();

	public DbSet<Match> Matches => Set<Match>();

	public DbSet<MatchPlayerStats> MatchPlayerStats => Set<MatchPlayerStats>();
	public DbSet<MatchSummaryStats> MatchSummaryStats => Set<MatchSummaryStats>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<User>()
			.HasOne(x => x.GameClub)
			.WithOne(x => x.User)
			.HasForeignKey<User>(x => x.GameClubId)
			.OnDelete(DeleteBehavior.SetNull);

		modelBuilder.Entity<UserClub>(entity =>
		{
			entity.HasKey(x => x.UserClubId);

			entity.HasIndex(x => new { x.UserId, x.SeasonId })
				.IsUnique();

			entity.HasOne(x => x.User)
				.WithMany(x => x.UserClubs)
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(x => x.Season)
				.WithMany(x => x.UserClubs)
				.HasForeignKey(x => x.SeasonId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(x => x.BaseGameClub)
				.WithMany()
				.HasForeignKey(x => x.BaseGameClubId)
				.OnDelete(DeleteBehavior.SetNull);

			entity.Property(x => x.Name)
				.HasMaxLength(100);

			entity.Property(x => x.LogoUrl)
				.HasMaxLength(500);

			entity.Property(x => x.InitialBudget)
				.HasPrecision(18, 2);

			entity.Property(x => x.CurrentBudget)
				.HasPrecision(18, 2);
		});

		modelBuilder.Entity<UserClubPlayer>(entity =>
		{
			entity.HasKey(x => x.UserClubPlayerId);

			entity.HasIndex(x => new { x.UserClubId, x.GamePlayerId })
				.IsUnique();

			entity.HasOne(x => x.UserClub)
				.WithMany(x => x.Players)
				.HasForeignKey(x => x.UserClubId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(x => x.GamePlayer)
				.WithMany(x => x.UserClubPlayers)
				.HasForeignKey(x => x.GamePlayerId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.Property(x => x.AcquisitionPrice)
				.HasPrecision(18, 2);

			entity.Property(x => x.AcquisitionType)
				.HasMaxLength(50);
		});

		modelBuilder.Entity<ClubLineup>(entity =>
		{
			entity.HasKey(x => x.ClubLineupId);

			entity.HasOne(x => x.UserClub)
				.WithMany(x => x.Lineups)
				.HasForeignKey(x => x.UserClubId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.Property(x => x.Formation)
				.HasMaxLength(50);
		});

		modelBuilder.Entity<ClubLineupPlayer>(entity =>
		{
			entity.HasKey(x => x.ClubLineupPlayerId);

			entity.HasIndex(x => new { x.ClubLineupId, x.SlotIndex })
				.IsUnique();

			entity.HasOne(x => x.ClubLineup)
				.WithMany(x => x.Players)
				.HasForeignKey(x => x.ClubLineupId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(x => x.UserClubPlayer)
				.WithMany(x => x.LineupPlayers)
				.HasForeignKey(x => x.UserClubPlayerId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.Property(x => x.Position)
				.HasMaxLength(20);
		});

		modelBuilder.Entity<TransferRecord>(entity =>
		{
			entity.HasKey(x => x.TransferRecordId);

			entity.HasOne(x => x.Season)
				.WithMany(x => x.TransferRecords)
				.HasForeignKey(x => x.SeasonId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(x => x.GamePlayer)
				.WithMany(x => x.TransferRecords)
				.HasForeignKey(x => x.GamePlayerId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(x => x.FromUserClub)
				.WithMany(x => x.TransfersOut)
				.HasForeignKey(x => x.FromUserClubId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(x => x.ToUserClub)
				.WithMany(x => x.TransfersIn)
				.HasForeignKey(x => x.ToUserClubId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.Property(x => x.TransferFee)
				.HasPrecision(18, 2);

			entity.Property(x => x.TransferType)
				.HasMaxLength(50);

			entity.Property(x => x.Notes)
				.HasMaxLength(500);
		});

		modelBuilder.Entity<UserClubBudgetTransaction>(entity =>
		{
			entity.HasKey(x => x.UserClubBudgetTransactionId);

			entity.HasOne(x => x.UserClub)
				.WithMany(x => x.BudgetTransactions)
				.HasForeignKey(x => x.UserClubId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(x => x.Season)
				.WithMany(x => x.UserClubBudgetTransactions)
				.HasForeignKey(x => x.SeasonId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.Property(x => x.Amount)
				.HasPrecision(18, 2);

			entity.Property(x => x.Type)
				.HasMaxLength(50);

			entity.Property(x => x.Description)
				.HasMaxLength(500);
		});

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

		modelBuilder.Entity<MatchSummaryStats>(entity =>
		{
			entity.HasKey(x => x.MatchSummaryStatsId);

			entity.HasOne(x => x.Match)
				.WithMany()
				.HasForeignKey(x => x.MatchId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<NotificationToken>(entity =>
		{
			entity.HasKey(x => x.NotificationTokenId);

			entity.HasIndex(x => x.FcmToken)
				.IsUnique();

			entity.HasOne(x => x.User)
				.WithMany(x => x.NotificationTokens)
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.Property(x => x.FcmToken)
				.HasMaxLength(500);

			entity.Property(x => x.DeviceType)
				.HasMaxLength(50);
		});

		modelBuilder.Entity<Message>(entity =>
		{
			entity.HasKey(x => x.MessageId);

			entity.Property(x => x.Text)
				.IsRequired()
				.HasMaxLength(1000);

			entity.HasOne(x => x.SenderUser)
				.WithMany()
				.HasForeignKey(x => x.SenderUserId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(x => x.ReceiverUser)
				.WithMany()
				.HasForeignKey(x => x.ReceiverUserId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasIndex(x => new { x.SenderUserId, x.ReceiverUserId });

			entity.HasIndex(x => new { x.ReceiverUserId, x.IsRead });
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
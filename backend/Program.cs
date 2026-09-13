using backend.Application.Stats;
using backend.Data;
using backend.Features.ActivityFeed.Services;
using backend.Features.Cloudinary.Services;
using backend.Features.Clubs.Services;
using backend.Features.Dashboard.Services;
using backend.Features.Draft.Services;
using backend.Features.GameData.Services;
using backend.Features.Matches.Services;
using backend.Features.Messages.Services;
using backend.Features.Notifications.Services;
using backend.Features.Rankings.Services;
using backend.Features.Realtime;
using backend.Features.Seasons.Services;
using backend.Features.Sidebar.Services;
using backend.Features.Stats.Services;
using backend.Features.Transfers.Services;
using backend.Features.Users.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseNpgsql(
		builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddSingleton<PresenceTracker>();
builder.Services.AddScoped<IAppRealtimeService, AppRealtimeService>();
builder.Services.AddScoped<ISeasonService, SeasonService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddHttpClient<IGameDataService, GameDataService>();
builder.Services.AddScoped<IStatsService, StatsService>();
builder.Services.AddScoped<IMatchStatsOcrService, MatchStatsOcrService>();
builder.Services.AddScoped<IClubService, ClubService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<IActivityFeedService, ActivityFeedService>();
builder.Services.AddScoped<IRankingService, RankingService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<ISidebarService, SidebarService>();
builder.Services.AddScoped<DraftService>();
builder.Services.AddScoped<IDraftService, DraftRealtimeDecorator>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<INotificationInboxService, NotificationInboxService>();
builder.Services.AddScoped<IMatchStatsService, MatchStatsService>();

// Settings
builder.Services.Configure<CloudinarySettings>(
	builder.Configuration.GetSection("Cloudinary"));

// Firebase Admin SDK
var firebaseAdminSdkJson = builder.Configuration["Firebase:AdminSdkJson"];

if (string.IsNullOrWhiteSpace(firebaseAdminSdkJson))
{
	throw new InvalidOperationException("Firebase Admin SDK JSON is missing. Set Firebase__AdminSdkJson.");
}

if (FirebaseApp.DefaultInstance is null)
{
	FirebaseApp.Create(new AppOptions
	{
		Credential = GoogleCredential.FromJson(firebaseAdminSdkJson)
	});
}

// CORS
builder.Services.AddCors(options =>
{
	options.AddPolicy("AngularPolicy", policy =>
	{
		policy
			.WithOrigins(
				"http://localhost:4200",
				"https://eleaguefutbol.com",
				"https://www.eleaguefutbol.com"
			)
			.AllowAnyHeader()
			.AllowAnyMethod();
	});
});

// Auth0
builder.Services
	.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.Authority =
			$"https://{builder.Configuration["Auth0:Domain"]}/";

		options.Audience =
			builder.Configuration["Auth0:Audience"];

		options.Events = new JwtBearerEvents
		{
			OnMessageReceived = context =>
			{
				var accessToken = context.Request.Query["access_token"];
				var path = context.HttpContext.Request.Path;

				if (!string.IsNullOrEmpty(accessToken) &&
					path.StartsWithSegments("/hubs"))
				{
					context.Token = accessToken;
				}

				return Task.CompletedTask;
			}
		};
	});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Render maneja HTTPS afuera.
// No usar app.UseHttpsRedirection() en Render.

app.UseCors("AngularPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AppHub>("/hubs/app");

app.Run();

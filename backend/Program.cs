using backend.Data;
using backend.Features.ActivityFeed.Services;
using backend.Features.Cloudinary.Services;
using backend.Features.Clubs.Services;
using backend.Features.Dashboard.Services;
using backend.Features.GameData.Services;
using backend.Features.Seasons.Services;
using backend.Features.Stats.Services;
using backend.Features.Users.Services;
using eLeagueFutbol.Application.Stats;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseNpgsql(
		builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ISeasonService, SeasonService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddHttpClient<IGameDataService, GameDataService>();
builder.Services.AddScoped<ISeasonService, SeasonService>();
builder.Services.AddScoped<IStatsService, StatsService>();
builder.Services.AddScoped<IMatchStatsOcrService, MatchStatsOcrService>();
builder.Services.AddScoped<IClubService, ClubService>();
builder.Services.Configure<CloudinarySettings>(
	builder.Configuration.GetSection("Cloudinary"));

builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<IActivityFeedService, ActivityFeedService>();

builder.Services.AddCors(options =>
{
	options.AddPolicy("AngularPolicy", policy =>
	{
		policy
	.WithOrigins(
		"http://localhost:4200",
		"https://eleaguefutbol.fidelfm.com"
	)
	.AllowAnyHeader()
	.AllowAnyMethod();
	});
});

builder.Services
	.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.Authority =
			$"https://{builder.Configuration["Auth0:Domain"]}/";

		options.Audience =
			builder.Configuration["Auth0:Audience"];
	});

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AngularPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
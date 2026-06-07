using backend.Data;
using backend.Features.Dashboard.Services;
using backend.Features.Seasons.Services;
using backend.Features.Users.Services;
using backend.Features.GameClubs.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;

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
//builder.Services.AddScoped<IGameClubService, GameClubService>();
builder.Services.AddHttpClient<IGameClubService, GameClubService>();
builder.Services.AddScoped<ISeasonService, SeasonService>();

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
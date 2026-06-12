using CsvHelper.Configuration.Attributes;

namespace backend.Features.GameData.Dtos;

public class Fc26PlayerCsvRow
{
	[Name("player_id")]
	public int SofifaPlayerId { get; set; }

	[Name("short_name")]
	public string Name { get; set; } = string.Empty;

	[Name("long_name")]
	public string FullName { get; set; } = string.Empty;

	[Name("overall")]
	public int Overall { get; set; }

	[Name("potential")]
	public int Potential { get; set; }

	[Name("player_positions")]
	public string Positions { get; set; } = string.Empty;

	[Name("club_team_id")]
	public int? ClubTeamId { get; set; }

	[Name("club_name")]
	public string? ClubName { get; set; }

	[Name("nationality_name")]
	public string Nation { get; set; } = string.Empty;

	[Name("player_face_url")]
	public string? ImageUrl { get; set; }

	[Name("age")]
	public int Age { get; set; }

	[Name("height_cm")]
	public int HeightCm { get; set; }

	[Name("weight_kg")]
	public int WeightKg { get; set; }

	[Name("preferred_foot")]
	public string PreferredFoot { get; set; } = string.Empty;
}
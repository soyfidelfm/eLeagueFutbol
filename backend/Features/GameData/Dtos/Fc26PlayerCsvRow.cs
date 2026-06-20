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

	[Name("player_positions")]
	public string Positions { get; set; } = string.Empty;

	[Name("overall")]
	public int Overall { get; set; }

	[Name("potential")]
	public int Potential { get; set; }

	[Name("value_eur")]
	public int? ValueEur { get; set; }

	[Name("wage_eur")]
	public int? WageEur { get; set; }

	[Name("age")]
	public int Age { get; set; }

	[Name("dob")]
	public DateTime? DateOfBirth { get; set; }

	[Name("height_cm")]
	public int HeightCm { get; set; }

	[Name("weight_kg")]
	public int WeightKg { get; set; }

	[Name("club_team_id")]
	public int? ClubTeamId { get; set; }

	[Name("club_name")]
	public string? ClubName { get; set; }

	[Name("club_jersey_number")]
	public int? ClubJerseyNumber { get; set; }

	[Name("club_contract_valid_until_year")]
	public int? ClubContractValidUntilYear { get; set; }

	[Name("nationality_id")]
	public int? NationalityId { get; set; }

	[Name("nationality_name")]
	public string Nation { get; set; } = string.Empty;

	[Name("nation_team_id")]
	public int? NationTeamId { get; set; }

	[Name("preferred_foot")]
	public string PreferredFoot { get; set; } = string.Empty;

	[Name("weak_foot")]
	public int? WeakFoot { get; set; }

	[Name("skill_moves")]
	public int? SkillMoves { get; set; }

	[Name("release_clause_eur")]
	public int? ReleaseClauseEur { get; set; }

	[Name("pace")]
	public int? Pace { get; set; }

	[Name("shooting")]
	public int? Shooting { get; set; }

	[Name("passing")]
	public int? Passing { get; set; }

	[Name("dribbling")]
	public int? Dribbling { get; set; }

	[Name("defending")]
	public int? Defending { get; set; }

	[Name("physic")]
	public int? Physic { get; set; }

	[Name("player_face_url")]
	public string? ImageUrl { get; set; }
}
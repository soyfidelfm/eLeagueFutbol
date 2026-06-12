namespace backend.Features.GameData.Dtos;

public class ImportGameClubsDto
{
	public int Total { get; set; }

	public List<JsonGameClubDto> Teams { get; set; } = new();
}

public class JsonGameClubDto
{
	public string Teamid { get; set; } = string.Empty;

	public string Slug { get; set; } = string.Empty;

	public JsonGameClubInfoDto Info { get; set; } = new();

	public JsonStadiumDto? Stadium { get; set; }
}

public class JsonGameClubInfoDto
{
	public string Teamname { get; set; } = string.Empty;

	public int Overallrating { get; set; }

	public int Attackrating { get; set; }

	public int Midfieldrating { get; set; }

	public int Defenserating { get; set; }

	public List<JsonLeagueDto> League { get; set; } = new();
}

public class JsonLeagueDto
{
	public string Leaguename { get; set; } = string.Empty;
}

public class JsonStadiumDto
{
	public string Stadiumname { get; set; } = string.Empty;
}
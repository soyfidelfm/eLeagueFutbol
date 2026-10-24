namespace backend.Features.Search.Dtos;

public class GlobalSearchResultDto
{
	public List<SearchPlayerDto> Players { get; set; } = [];
	public List<SearchClubDto> Clubs { get; set; } = [];
	public List<SearchMatchDto> Matches { get; set; } = [];
}

public class SearchPlayerDto
{
	public Guid UserId { get; set; }
	public string DisplayName { get; set; } = string.Empty;
	public string GamerTag { get; set; } = string.Empty;
	public string AvatarUrl { get; set; } = string.Empty;
	public string ClubName { get; set; } = string.Empty;
	public bool IsMe { get; set; }
}

public class SearchClubDto
{
	public Guid ClubId { get; set; }
	public Guid SeasonId { get; set; }
	public string Name { get; set; } = string.Empty;
	public string LogoUrl { get; set; } = string.Empty;
	public string OwnerName { get; set; } = string.Empty;
	public bool IsMine { get; set; }
}

public class SearchMatchDto
{
	public Guid MatchId { get; set; }
	public Guid SeasonId { get; set; }
	public int WeekNumber { get; set; }
	public string HomeName { get; set; } = string.Empty;
	public string AwayName { get; set; } = string.Empty;
	public int HomeScore { get; set; }
	public int AwayScore { get; set; }
	public string Status { get; set; } = string.Empty;
	public DateTime MatchDate { get; set; }
}

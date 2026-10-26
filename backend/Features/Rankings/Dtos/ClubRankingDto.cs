namespace backend.Features.Rankings.DTOs;

public sealed class ClubRankingDto
{
	public int Rank { get; set; }

	public Guid ClubId { get; set; }
	public string ClubName { get; set; } = string.Empty;
	public string ClubLogoUrl { get; set; } = string.Empty;

	public int Points { get; set; }

	public int Wins { get; set; }
	public int Draws { get; set; }
	public int Losses { get; set; }

	public int GoalsFor { get; set; }
	public int GoalsAgainst { get; set; }
	public int GoalDifference { get; set; }

	public int Played { get; set; }

	public decimal WinRate { get; set; }

	public List<string> Form { get; set; } = [];
}
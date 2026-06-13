namespace backend.Features.Rankings.DTOs;

public sealed class PlayerRankingDto
{
	public int Rank { get; set; }

	public Guid UserId { get; set; }
	public string PlayerName { get; set; } = string.Empty;
	public string AvatarUrl { get; set; } = string.Empty;

	public Guid? ClubId { get; set; }
	public string ClubName { get; set; } = string.Empty;
	public string ClubLogoUrl { get; set; } = string.Empty;

	public int Points { get; set; }
	public int Wins { get; set; }
	public int Losses { get; set; }

	public int GoalsFor { get; set; }
	public int GoalsAgainst { get; set; }

	public decimal WinRate { get; set; }

	public List<string> Form { get; set; } = [];
}
namespace backend.Application.Stats.DTOs;

public class TopPlayerDto
{
	public int Rank { get; set; }
	public Guid UserId { get; set; }
	public string GamerTag { get; set; } = string.Empty;
	public string ClubName { get; set; } = string.Empty;
	public string AvatarUrl { get; set; } = string.Empty;

	public int Matches { get; set; }
	public int Goals { get; set; }
	public int Assists { get; set; }
	public decimal Rating { get; set; }
}
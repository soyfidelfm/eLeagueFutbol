namespace backend.Features.Draft.Dtos;

public class DraftStateDto
{
	public Guid DraftSessionId { get; set; }

	public bool IsActive { get; set; }

	public int CurrentRound { get; set; }

	public int CurrentPickNumber { get; set; }

	public Guid? CurrentUserId { get; set; }

	public string CurrentClubName { get; set; } = string.Empty;

	public int SecondsRemaining { get; set; }

	public int TotalTeams { get; set; }

	public int PlayersPerTeam { get; set; }
}
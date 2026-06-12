namespace backend.Application.MatchStats.DTOs;

public class MatchPlayerStatsOcrPreviewDto
{
	public string Position { get; set; } = string.Empty;
	public string PlayerName { get; set; } = string.Empty;

	public decimal Rating { get; set; }

	public int Goals { get; set; }
	public int Assists { get; set; }

	public int Shots { get; set; }
	public decimal ShotAccuracy { get; set; }

	public int Passes { get; set; }
	public decimal PassAccuracy { get; set; }

	public int Dribbles { get; set; }
	public decimal DribbleSuccessRate { get; set; }

	public int Tackles { get; set; }
	public decimal TackleSuccessRate { get; set; }

	public int Offsides { get; set; }
	public int FoulsCommitted { get; set; }

	public int PossessionWon { get; set; }
	public int PossessionLost { get; set; }

	public int MinutesPlayed { get; set; }

	public decimal DistanceCoveredKm { get; set; }
	public decimal DistanceSprintedKm { get; set; }

	public bool IsPlayerOfTheMatch { get; set; }
}
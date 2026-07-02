namespace backend.Features.Clubs.Dtos;

public class MyClubDto
{
	public Guid ClubId { get; set; }

	public string Name { get; set; } = string.Empty;
	public string? Abbreviation { get; set; }
	public string? LogoUrl { get; set; }
	public string? Description { get; set; }
	public string? Slogan { get; set; }
	public string? Region { get; set; }
	public string? Division { get; set; }

	public int Members { get; set; }

	public int MatchesPlayed { get; set; }
	public int Wins { get; set; }
	public int Losses { get; set; }
	public int Draws { get; set; }
	public int GoalsFor { get; set; }
	public int GoalsAgainst { get; set; }
	public int GoalDifference { get; set; }
	public int Rating { get; set; }

	public int Chemistry { get; set; }

	public string Formation { get; set; } = "4-3-3 Control";

	/// <summary>
	/// Toda la plantilla del club.
	/// </summary>
	public List<MyClubPlayerDto> Players { get; set; } = [];

	/// <summary>
	/// Solamente el XI inicial.
	/// </summary>
	public List<MyClubPlayerDto> StartingPlayers { get; set; } = [];

	public List<MyClubActivityDto> Activities { get; set; } = [];
}

public class MyClubPlayerDto
{
	public Guid PlayerId { get; set; }

	public string Name { get; set; } = string.Empty;
	public string? Position { get; set; }
	public int? Overall { get; set; }
	public string? AvatarUrl { get; set; }

	// Para reconstruir la alineación guardada
	public int SlotIndex { get; set; }

	// Para chemistry
	public string? Nation { get; set; }
	public string? League { get; set; }

	// Chemistry individual (0-3)
	public int Chemistry { get; set; }

	// Coordenadas para dibujar la cancha
	public int X { get; set; }
	public int Y { get; set; }
}

public class MyClubActivityDto
{
	public Guid Id { get; set; }

	public string Type { get; set; } = string.Empty;
	public string Text { get; set; } = string.Empty;
	public string Icon { get; set; } = string.Empty;

	public DateTime CreatedAtUTC { get; set; }
}
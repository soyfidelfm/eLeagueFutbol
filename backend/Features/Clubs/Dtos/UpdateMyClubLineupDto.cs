namespace backend.Features.Clubs.Dtos;

public class UpdateMyClubLineupDto
{
	public string Formation { get; set; } = string.Empty;

	public List<UpdateMyClubLineupPlayerDto> Players { get; set; } = [];
}

public class UpdateMyClubLineupPlayerDto
{
	public Guid PlayerId { get; set; }

	public int SlotIndex { get; set; }

	public string? Position { get; set; }
}
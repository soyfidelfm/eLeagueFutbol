namespace backend.Features.Seasons.Dtos;

public class SeasonRegistrationDto
{
	public Guid SeasonRegistrationId { get; set; }
	public Guid SeasonId { get; set; }
	public Guid UserId { get; set; }
	public Guid GameClubId { get; set; }
	public DateTime RegisteredAtUTC { get; set; }
}
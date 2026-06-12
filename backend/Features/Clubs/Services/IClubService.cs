using backend.Features.Clubs.Dtos;

namespace backend.Features.Clubs.Services;

public interface IClubService
{
	Task<MyClubDto?> GetMyClubAsync(Guid userId);
	Task<MyClubDto?> GetMyClubAsyncByAuth0Id(string auth0Id);
	Task<MyClubDto> UpdateClubLogoAsync(string auth0Id, string logoUrl);
	Task<MyClubDto?> UpdateMyClubLineupAsync(string auth0Id, UpdateMyClubLineupDto dto);
}
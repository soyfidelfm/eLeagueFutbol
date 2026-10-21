using backend.Features.Search.Dtos;

namespace backend.Features.Search.Services;

public interface ISearchService
{
	Task<GlobalSearchResultDto> SearchAsync(string auth0Id, string? query);
}

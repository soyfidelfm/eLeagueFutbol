namespace backend.Features.Search.Controllers;

using System.Security.Claims;
using backend.Features.Search.Dtos;
using backend.Features.Search.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/search")]
[Authorize]
public class SearchController : ControllerBase
{
	private readonly ISearchService _searchService;

	public SearchController(ISearchService searchService)
	{
		_searchService = searchService;
	}

	/// <summary>Global search: up to 5 players, clubs and matches.</summary>
	[HttpGet]
	public async Task<ActionResult<GlobalSearchResultDto>> Search([FromQuery] string? q)
	{
		var auth0Id =
			User.FindFirstValue(ClaimTypes.NameIdentifier) ??
			User.FindFirstValue("sub") ??
			string.Empty;

		if (string.IsNullOrWhiteSpace(auth0Id))
			return Unauthorized();

		return Ok(await _searchService.SearchAsync(auth0Id, q));
	}
}

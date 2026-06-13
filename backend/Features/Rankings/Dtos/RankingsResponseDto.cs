using backend.Features.Rankings.Dtos;

namespace backend.Features.Rankings.DTOs;

public sealed class RankingsResponseDto
{
	public Guid SeasonId { get; set; }
	public string SeasonName { get; set; } = string.Empty;

	public List<RankingSeasonDto> Seasons { get; set; } = [];

	public List<PlayerRankingDto> PlayerRankings { get; set; } = [];
	public List<ClubRankingDto> ClubRankings { get; set; } = [];
}
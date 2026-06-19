namespace backend.Application.Stats.DTOs;

public class MatchOcrPreviewDto
{
	public string HomeTeamName { get; set; } = string.Empty;
	public string AwayTeamName { get; set; } = string.Empty;

	public int? HomeScore { get; set; }
	public int? AwayScore { get; set; }

	public List<MatchOcrEventPreviewDto> Events { get; set; } = [];
	public List<string> RawLines { get; set; } = [];
	public List<string> Warnings { get; set; } = [];
}
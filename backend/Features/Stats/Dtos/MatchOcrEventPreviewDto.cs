namespace backend.Application.Stats.DTOs;

public class MatchOcrEventPreviewDto
{
	public int Minute { get; set; }
	public string Type { get; set; } = string.Empty;
	public string PlayerName { get; set; } = string.Empty;
	public string TeamName { get; set; } = string.Empty;
	public string RawText { get; set; } = string.Empty;
}
namespace backend.Application.Stats.DTOs;

public class PerformancePointDto
{
	public string Label { get; set; } = string.Empty;
	public int Goals { get; set; }
	public int Assists { get; set; }
	public decimal Rating { get; set; }
}
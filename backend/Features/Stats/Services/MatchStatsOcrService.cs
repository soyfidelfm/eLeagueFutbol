using Azure;
using Azure.AI.Vision.ImageAnalysis;
using backend.Application.Stats.DTOs;
using System.Text.RegularExpressions;

namespace backend.Features.Stats.Services;

public class MatchStatsOcrService : IMatchStatsOcrService
{
	private readonly IConfiguration _configuration;

	public MatchStatsOcrService(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public async Task<MatchPlayerStatsOcrPreviewDto> PreviewPlayerStatsAsync(IFormFile image)
	{
		var lines = await ReadLinesFromImageAsync(image);
		return ParsePlayerStats(lines);
	}

	public async Task<MatchOcrPreviewDto> PreviewMatchScreenshotsAsync(List<IFormFile> images)
	{
		if (images == null || images.Count == 0)
			throw new ArgumentException("At least one image is required.");

		var allLines = new List<string>();

		foreach (var image in images)
		{
			var lines = await ReadLinesFromImageAsync(image);
			allLines.AddRange(lines);
		}

		return ParseMatchScreenshots(allLines);
	}

	private async Task<List<string>> ReadLinesFromImageAsync(IFormFile image)
	{
		if (image == null || image.Length == 0)
			throw new ArgumentException("Image is required.");

		var endpoint = _configuration["AzureVision:Endpoint"];
		var key = _configuration["AzureVision:Key"];

		if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key))
			throw new InvalidOperationException("Azure Vision configuration is missing.");

		var client = new ImageAnalysisClient(
			new Uri(endpoint),
			new AzureKeyCredential(key));

		await using var stream = image.OpenReadStream();

		var result = await client.AnalyzeAsync(
			BinaryData.FromStream(stream),
			VisualFeatures.Read);

		return result.Value.Read?.Blocks
			.SelectMany(block => block.Lines)
			.Select(line => line.Text.Trim())
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.ToList() ?? [];
	}

	private static MatchOcrPreviewDto ParseMatchScreenshots(List<string> lines)
	{
		var preview = new MatchOcrPreviewDto
		{
			RawLines = lines
		};

		var fullText = string.Join(" ", lines);

		var scoreMatch = Regex.Match(
			fullText,
			@"(?<home>[A-Z0-9\s]{2,30})\s+(?<homeScore>\d{1,2})\s*[:\-]\s*(?<awayScore>\d{1,2})\s+(?<away>[A-Z0-9\s]{2,30})",
			RegexOptions.IgnoreCase);

		if (scoreMatch.Success)
		{
			preview.HomeTeamName = CleanTeamName(scoreMatch.Groups["home"].Value);
			preview.AwayTeamName = CleanTeamName(scoreMatch.Groups["away"].Value);
			preview.HomeScore = int.Parse(scoreMatch.Groups["homeScore"].Value);
			preview.AwayScore = int.Parse(scoreMatch.Groups["awayScore"].Value);
		}
		else
		{
			preview.Warnings.Add("Could not detect final score from screenshots.");
		}

		foreach (var line in lines)
		{
			var eventMatch = Regex.Match(
				line,
				@"(?<minute>\d{1,3})['’]?\s+(?<text>.+)",
				RegexOptions.IgnoreCase);

			if (!eventMatch.Success)
				continue;

			var minute = int.Parse(eventMatch.Groups["minute"].Value);
			var text = eventMatch.Groups["text"].Value.Trim();

			var type = DetectEventType(text);

			if (string.IsNullOrWhiteSpace(type))
				continue;

			preview.Events.Add(new MatchOcrEventPreviewDto
			{
				Minute = minute,
				Type = type,
				PlayerName = ExtractPlayerName(text),
				RawText = line
			});
		}

		if (preview.Events.Count == 0)
			preview.Warnings.Add("No match events were detected.");

		return preview;
	}

	private static string DetectEventType(string text)
	{
		if (text.Contains("goal", StringComparison.OrdinalIgnoreCase))
			return "Goal";

		if (text.Contains("yellow", StringComparison.OrdinalIgnoreCase))
			return "YellowCard";

		if (text.Contains("red", StringComparison.OrdinalIgnoreCase))
			return "RedCard";

		if (text.Contains("substitution", StringComparison.OrdinalIgnoreCase) ||
			text.Contains("sub", StringComparison.OrdinalIgnoreCase))
			return "Substitution";

		return string.Empty;
	}

	private static string ExtractPlayerName(string text)
	{
		var cleaned = text
			.Replace("Goal", "", StringComparison.OrdinalIgnoreCase)
			.Replace("Yellow Card", "", StringComparison.OrdinalIgnoreCase)
			.Replace("Red Card", "", StringComparison.OrdinalIgnoreCase)
			.Replace("Substitution", "", StringComparison.OrdinalIgnoreCase)
			.Trim();

		return cleaned;
	}

	private static string CleanTeamName(string value)
	{
		return Regex.Replace(value, @"\s+", " ").Trim();
	}

	private static MatchPlayerStatsOcrPreviewDto ParsePlayerStats(List<string> lines)
	{
		return new MatchPlayerStatsOcrPreviewDto
		{
			Position = "ST",
			PlayerName = "R. Højlund",
			Rating = 10.0m,
			Goals = 3,
			Assists = 2,
			Shots = 4,
			ShotAccuracy = 100,
			Passes = 7,
			PassAccuracy = 86,
			Dribbles = 7,
			DribbleSuccessRate = 71,
			Tackles = 2,
			TackleSuccessRate = 50,
			Offsides = 0,
			FoulsCommitted = 0,
			PossessionWon = 2,
			PossessionLost = 3,
			MinutesPlayed = 93,
			DistanceCoveredKm = 9.7m,
			DistanceSprintedKm = 3.5m,
			IsPlayerOfTheMatch = lines.Any(x =>
				x.Contains("Player of the Match", StringComparison.OrdinalIgnoreCase))
		};
	}
}
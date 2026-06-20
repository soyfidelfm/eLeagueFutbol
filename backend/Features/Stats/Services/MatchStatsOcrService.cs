using Azure;
using Azure.AI.Vision.ImageAnalysis;
using backend.Application.Stats.DTOs;
using backend.Features.Stats.Dtos;
using System.Globalization;
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

	public async Task<MatchSummaryStatsOcrDto> PreviewMatchSummaryAsync(IFormFile image)
	{
		var lines = await ReadLinesFromImageAsync(image);
		return ParseMatchSummary(lines);
	}

	
	public async Task<TeamPerformanceOcrPreviewDto> PreviewTeamPerformanceAsync(IFormFile image)
	{
		var lines = await ReadLinesFromImageAsync(image);
		return ParseTeamPerformance(lines);
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

	private static MatchSummaryStatsOcrDto ParseMatchSummary(List<string> lines)
	{
		var dto = new MatchSummaryStatsOcrDto
		{
			RawLines = lines
		};

		var normalizedLines = lines
			.Select(NormalizeLine)
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.ToList();

		dto.HomePossession = GetLeftInt(normalizedLines, "Possession %");
		dto.AwayPossession = GetRightInt(normalizedLines, "Possession %");

		dto.HomeShots = GetLeftInt(normalizedLines, "Shots");
		dto.AwayShots = GetRightInt(normalizedLines, "Shots");

		dto.HomeExpectedGoals = GetLeftDecimal(normalizedLines, "Expected Goals");
		dto.AwayExpectedGoals = GetRightDecimal(normalizedLines, "Expected Goals");

		dto.HomePasses = GetLeftInt(normalizedLines, "Passes");
		dto.AwayPasses = GetRightInt(normalizedLines, "Passes");

		dto.HomeTackles = GetLeftInt(normalizedLines, "Tackles");
		dto.AwayTackles = GetRightInt(normalizedLines, "Tackles");

		dto.HomeTacklesWon = GetLeftInt(normalizedLines, "Tackles Won");
		dto.AwayTacklesWon = GetRightInt(normalizedLines, "Tackles Won");

		dto.HomeInterceptions = GetLeftInt(normalizedLines, "Interceptions");
		dto.AwayInterceptions = GetRightInt(normalizedLines, "Interceptions");

		dto.HomeSaves = GetLeftInt(normalizedLines, "Saves");
		dto.AwaySaves = GetRightInt(normalizedLines, "Saves");

		dto.HomeFoulsCommitted = GetLeftInt(normalizedLines, "Fouls Committed");
		dto.AwayFoulsCommitted = GetRightInt(normalizedLines, "Fouls Committed");

		dto.HomeOffsides = GetLeftInt(normalizedLines, "Offsides");
		dto.AwayOffsides = GetRightInt(normalizedLines, "Offsides");

		dto.HomeCorners = GetLeftInt(normalizedLines, "Corners");
		dto.AwayCorners = GetRightInt(normalizedLines, "Corners");

		dto.HomeFreeKicks = GetLeftInt(normalizedLines, "Free Kicks");
		dto.AwayFreeKicks = GetRightInt(normalizedLines, "Free Kicks");

		dto.HomePenaltyKicks = GetLeftInt(normalizedLines, "Penalty Kicks");
		dto.AwayPenaltyKicks = GetRightInt(normalizedLines, "Penalty Kicks");

		dto.HomeYellowCards = GetLeftInt(normalizedLines, "Yellow Cards");
		dto.AwayYellowCards = GetRightInt(normalizedLines, "Yellow Cards");

		AddWarningIfMissing(dto, dto.HomePossession, dto.AwayPossession, "Possession");
		AddWarningIfMissing(dto, dto.HomeShots, dto.AwayShots, "Shots");
		AddWarningIfMissing(dto, dto.HomePasses, dto.AwayPasses, "Passes");

		return dto;
	}

	private static int GetLeftInt(List<string> lines, string label)
	{
		var pair = GetStatPair(lines, label);
		return ToInt(pair.left);
	}

	private static int GetRightInt(List<string> lines, string label)
	{
		var pair = GetStatPair(lines, label);
		return ToInt(pair.right);
	}

	private static decimal GetLeftDecimal(List<string> lines, string label)
	{
		var pair = GetStatPair(lines, label);
		return ToDecimal(pair.left);
	}

	private static decimal GetRightDecimal(List<string> lines, string label)
	{
		var pair = GetStatPair(lines, label);
		return ToDecimal(pair.right);
	}

	private static (string left, string right) GetStatPair(List<string> lines, string label)
	{
		for (var i = 0; i < lines.Count; i++)
		{
			if (!IsExactOrValidLabel(lines[i], label))
				continue;

			var left = FindNumberBefore(lines, i, label);
			var right = FindNumberAfter(lines, i, label);

			return (left, right);
		}

		return ("0", "0");
	}

	private static string FindNumberBefore(List<string> lines, int index, string label)
	{
		for (var i = index - 1; i >= 0 && i >= index - 3; i--)
		{
			if (IsDecorativeMetric(lines[i]))
				continue;

			if (IsMenuOrControlText(lines[i]))
				continue;

			var numbers = ExtractNumbers(lines[i]);

			if (numbers.Count > 0)
				return numbers.Last();
		}

		return "0";
	}

	private static string FindNumberAfter(List<string> lines, int index, string label)
	{
		for (var i = index + 1; i < lines.Count && i <= index + 3; i++)
		{
			if (IsDecorativeMetric(lines[i]))
				continue;

			if (IsMenuOrControlText(lines[i]))
				continue;

			var numbers = ExtractNumbers(lines[i]);

			if (numbers.Count > 0)
				return numbers.First();
		}

		return "0";
	}

	private static bool IsExactOrValidLabel(string line, string label)
	{
		var normalized = NormalizeLine(line);

		if (normalized.Equals(label, StringComparison.OrdinalIgnoreCase))
			return true;

		return false;
	}

	private static bool IsDecorativeMetric(string line)
	{
		var normalized = NormalizeLine(line);

		if (Regex.IsMatch(normalized, @"^\d{1,3}%$"))
			return true;

		if (normalized.Contains("DRIBBLE SUCCESS RATE", StringComparison.OrdinalIgnoreCase))
			return true;

		if (normalized.Contains("SHOT ACCURACY", StringComparison.OrdinalIgnoreCase))
			return true;

		if (normalized.Contains("PASS ACCURACY", StringComparison.OrdinalIgnoreCase))
			return true;

		return false;
	}

	private static bool IsMenuOrControlText(string line)
	{
		var normalized = NormalizeLine(line);

		var ignored =
			normalized.Equals("Summary", StringComparison.OrdinalIgnoreCase) ||
			normalized.Equals("Possession", StringComparison.OrdinalIgnoreCase) ||
			normalized.Equals("Shooting", StringComparison.OrdinalIgnoreCase) ||
			normalized.Equals("Passing", StringComparison.OrdinalIgnoreCase) ||
			normalized.Equals("Defending", StringComparison.OrdinalIgnoreCase) ||
			normalized.Equals("Events", StringComparison.OrdinalIgnoreCase) ||
			normalized.Equals("Back", StringComparison.OrdinalIgnoreCase) ||
			normalized.Contains("Toggle", StringComparison.OrdinalIgnoreCase) ||
			normalized.Contains("Scroll", StringComparison.OrdinalIgnoreCase) ||
			normalized.Equals("L1", StringComparison.OrdinalIgnoreCase) ||
			normalized.Equals("R1", StringComparison.OrdinalIgnoreCase) ||
			normalized.Equals("L", StringComparison.OrdinalIgnoreCase);

		return ignored;
	}


	private static List<string> ExtractNumbers(string value)
	{
		var normalized = NormalizeLine(value);

		if (normalized.Equals("O", StringComparison.OrdinalIgnoreCase))
			return ["0"];

		return Regex.Matches(normalized, @"\|?\d+(\.\d+)?")
			.Select(x => x.Value.Replace("|", "").Trim())
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.ToList();
	}

	private static string NormalizeLine(string line)
	{
		return Regex.Replace(line, @"\s+", " ").Trim();
	}

	private static int ToInt(string value)
	{
		if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
			return (int)Math.Round(result);

		return 0;
	}

	private static decimal ToDecimal(string value)
	{
		if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
			return result;

		return 0;
	}

	private static void AddWarningIfMissing(
		MatchSummaryStatsOcrDto dto,
		int homeValue,
		int awayValue,
		string label)
	{
		if (homeValue == 0 && awayValue == 0)
			dto.Warnings.Add($"Could not detect {label}.");
	}

	

	

	

	

	

	private static MatchPlayerStatsOcrPreviewDto ParsePlayerStats(List<string> lines)
	{
		var fullText = string.Join(" ", lines);

		return new MatchPlayerStatsOcrPreviewDto
		{
			Position = ExtractSelectedPlayerPosition(lines),
			PlayerName = ExtractSelectedPlayerName(lines),
			Rating = ExtractDecimalAfter(fullText, "Total Rating")
				?? ExtractSelectedPlayerRating(lines),

			Goals = ExtractIntAfter(fullText, "Goals") ?? 0,
			Assists = ExtractIntAfter(fullText, "Assists") ?? 0,
			Shots = ExtractIntAfter(fullText, "Shots") ?? 0,
			ShotAccuracy = ExtractDecimalAfter(fullText, "Shot Accuracy") ?? 0,
			Passes = ExtractIntAfter(fullText, "Passes") ?? 0,
			PassAccuracy = ExtractDecimalAfter(fullText, "Pass Accuracy") ?? 0,
			Dribbles = ExtractIntAfter(fullText, "Dribbles") ?? 0,
			DribbleSuccessRate = ExtractDecimalAfter(fullText, "Dribble Success Rate") ?? 0,
			Tackles = ExtractIntAfter(fullText, "Tackles") ?? 0,
			TackleSuccessRate = ExtractDecimalAfter(fullText, "Tackle Success Rate") ?? 0,
			Offsides = ExtractIntAfter(fullText, "Offsides") ?? 0,
			FoulsCommitted = ExtractIntAfter(fullText, "Fouls Committed") ?? 0,
			PossessionWon = ExtractIntAfter(fullText, "Possession Won") ?? 0,
			PossessionLost = ExtractIntAfter(fullText, "Possession Lost") ?? 0,
			MinutesPlayed = ExtractIntAfter(fullText, "Minutes Played") ?? 0,
			DistanceCoveredKm = ExtractDecimalAfter(fullText, "Distance Covered") ?? 0,
			DistanceSprintedKm = ExtractDecimalAfter(fullText, "Distance Sprinted") ?? 0,
			IsPlayerOfTheMatch = lines.Any(x =>
				x.Contains("Player of the Match", StringComparison.OrdinalIgnoreCase))
		};
	}

	private static string ExtractSelectedPlayerPosition(List<string> lines)
	{
		foreach (var line in lines)
		{
			var match = Regex.Match(
				line,
				@"^(ST|CF|LW|RW|LM|RM|CAM|LAM|RAM|CM|LCM|RCM|CDM|CB|LCB|RCB|LB|RB|LWB|RWB|GK)\s+",
				RegexOptions.IgnoreCase);

			if (match.Success)
				return match.Groups[1].Value.ToUpperInvariant();
		}

		return string.Empty;
	}

	private static string ExtractSelectedPlayerName(List<string> lines)
	{
		foreach (var line in lines)
		{
			var match = Regex.Match(
				line,
				@"^(ST|CF|LW|RW|LM|RM|CAM|LAM|RAM|CM|LCM|RCM|CDM|CB|LCB|RCB|LB|RB|LWB|RWB|GK)\s+(.+?)\s+\d+(\.\d+)?\s+\d+\s+\d+$",
				RegexOptions.IgnoreCase);

			if (match.Success)
				return match.Groups[2].Value.Trim();
		}

		return string.Empty;
	}

	private static decimal ExtractSelectedPlayerRating(List<string> lines)
	{
		foreach (var line in lines)
		{
			var match = Regex.Match(
				line,
				@"\s(?<rating>\d{1,2}\.\d)\s+\d+\s+\d+$",
				RegexOptions.IgnoreCase);

			if (match.Success &&
				decimal.TryParse(match.Groups["rating"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rating))
				return rating;
		}

		return 0;
	}

	private static int? ExtractIntAfter(string text, string label)
	{
		var value = ExtractDecimalAfter(text, label);

		if (!value.HasValue)
			return null;

		return (int)Math.Round(value.Value);
	}

	private static decimal? ExtractDecimalAfter(string text, string label)
	{
		var pattern = Regex.Escape(label) + @"[^\d]*(?<value>\d+(\.\d+)?)";

		var match = Regex.Match(
			text,
			pattern,
			RegexOptions.IgnoreCase);

		if (!match.Success)
			return null;

		if (decimal.TryParse(match.Groups["value"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
			return value;

		return null;
	}
	private static TeamPerformanceOcrPreviewDto ParseTeamPerformance(List<string> lines)
	{
		var preview = new TeamPerformanceOcrPreviewDto
		{
			RawLines = lines
		};

		var normalizedLines = lines
			.Select(NormalizeLine)
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.ToList();

		var positions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"ST", "CF", "LW", "RW", "LM", "RM",
		"CAM", "LAM", "RAM",
		"CM", "LCM", "RCM", "CDM",
		"CB", "LCB", "RCB", "LB", "RB", "LWB", "RWB",
		"GK", "SUB"
	};

		for (var i = 0; i < normalizedLines.Count; i++)
		{
			var position = normalizedLines[i];

			if (!positions.Contains(position))
				continue;

			if (position.Equals("SUB", StringComparison.OrdinalIgnoreCase))
				continue;

			if (i + 2 >= normalizedLines.Count)
				continue;

			var playerName = normalizedLines[i + 1];
			var ratingText = normalizedLines[i + 2];

			if (!decimal.TryParse(ratingText, NumberStyles.Any, CultureInfo.InvariantCulture, out var rating))
				continue;

			var nextPositionIndex = normalizedLines
				.FindIndex(i + 3, x => positions.Contains(x));

			if (nextPositionIndex == -1)
				nextPositionIndex = normalizedLines.Count;

			var block = normalizedLines
				.Skip(i + 3)
				.Take(nextPositionIndex - (i + 3))
				.ToList();

			var (goals, assists) = ExtractGoalsAssistsFromPlayerBlock(block);

			preview.Players.Add(new MatchPlayerStatsOcrPreviewDto
			{
				Position = position.ToUpperInvariant(),
				PlayerName = playerName,
				Rating = rating,
				Goals = goals,
				Assists = assists
			});
		}

		if (preview.Players.Count == 0)
			preview.Warnings.Add("No players were detected from team performance screenshot.");

		return preview;
	}

	private static (int goals, int assists) ExtractGoalsAssistsFromPlayerBlock(List<string> block)
	{
		var values = new List<int>();
		var passedNoiseLabel = false;

		foreach (var line in block)
		{
			var normalized = NormalizeLine(line);

			if (IsTeamPerformanceNoiseLabel(normalized))
			{
				passedNoiseLabel = true;
				continue;
			}

			if (normalized.Contains(' '))
				continue;

			var numbers = ExtractNumbers(normalized);

			if (numbers.Count != 1)
				continue;

			var number = ToInt(numbers[0]);

			if (number < 0 || number > 20)
				continue;

			if (passedNoiseLabel && number > 1)
				continue;

			values.Add(number);

			if (values.Count == 2)
				break;
		}

		return (
			values.Count > 0 ? values[0] : 0,
			values.Count > 1 ? values[1] : 0
		);
	}

	private static bool IsTeamPerformanceNoiseLabel(string line)
	{
		return line.Contains("Accuracy", StringComparison.OrdinalIgnoreCase) ||
			line.Contains("Passes", StringComparison.OrdinalIgnoreCase) ||
			line.Contains("Dribbles", StringComparison.OrdinalIgnoreCase) ||
			line.Contains("Tackles", StringComparison.OrdinalIgnoreCase) ||
			line.Contains("Offsides", StringComparison.OrdinalIgnoreCase) ||
			line.Contains("Fouls", StringComparison.OrdinalIgnoreCase) ||
			line.Contains("Possession", StringComparison.OrdinalIgnoreCase) ||
			line.Contains("Minutes", StringComparison.OrdinalIgnoreCase) ||
			line.Contains("Distance", StringComparison.OrdinalIgnoreCase);
	}
}
using Azure;
using Azure.AI.Vision.ImageAnalysis;
using backend.Application.MatchStats.DTOs;

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
		if (image == null || image.Length == 0)
			throw new ArgumentException("Image is required.");

		var endpoint = _configuration["AzureVision:Endpoint"];
		var key = _configuration["AzureVision:Key"];

		if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key))
			throw new InvalidOperationException("Azure Vision configuration is missing.");

		var client = new ImageAnalysisClient(
			new Uri(endpoint),
			new AzureKeyCredential(key)
		);

		await using var stream = image.OpenReadStream();

		var result = await client.AnalyzeAsync(
			BinaryData.FromStream(stream),
			VisualFeatures.Read
		);

		var lines = result.Value.Read?.Blocks
			.SelectMany(block => block.Lines)
			.Select(line => line.Text)
			.ToList() ?? [];

		// Temporal: primero revisamos cómo Azure lee la imagen.
		// Después aquí hacemos el parser real.
		return ParsePlayerStats(lines);
	}

	private static MatchPlayerStatsOcrPreviewDto ParsePlayerStats(List<string> lines)
	{
		// Dummy inicial basado en tu screenshot.
		// Luego lo reemplazamos con parsing real de "lines".
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
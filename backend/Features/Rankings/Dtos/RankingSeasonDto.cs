namespace backend.Features.Rankings.Dtos
{
	public sealed class RankingSeasonDto
	{
		public Guid SeasonId { get; set; }
		public string Name { get; set; } = string.Empty;
		public bool IsCurrent { get; set; }
	}
}

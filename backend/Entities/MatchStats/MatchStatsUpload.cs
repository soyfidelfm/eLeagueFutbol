using backend.Entities.Common;
using backend.Entities.Enums;
using backend.Entities.Tournaments;

namespace backend.Entities.MatchStats
{
	public class MatchStatsUpload :BaseEntity
	{
		public Guid MatchStatsUploadId { get; set; }

		public Guid MatchId { get; set; }
		public Guid UploadedByUserId { get; set; }

		public MatchStatsUploadType Type { get; set; }

		public string ImageUrl { get; set; } = string.Empty;
		public string? RawJson { get; set; }

		public bool IsProcessed { get; set; }

		public Match Match { get; set; } = null!;
	}
}

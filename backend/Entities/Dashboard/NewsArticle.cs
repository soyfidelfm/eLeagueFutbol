namespace backend.Entities.Dashboard;

public class NewsArticle
{
	public Guid Id { get; set; }

	public string Title { get; set; } = string.Empty;

	public string Content { get; set; } = string.Empty;

	public bool IsPublished { get; set; } = true;

	public DateTime CreatedAtUTC { get; set; }
}
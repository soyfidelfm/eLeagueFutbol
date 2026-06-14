namespace backend.Features.Sidebar.Dtos;

public class SidebarDto
{
	public SidebarSeasonDto? Season { get; set; }
	public SidebarCupDto? Cup { get; set; }
}

public class SidebarSeasonDto
{
	public string Label { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public int TotalClubs { get; set; }
	public int TotalMatches { get; set; }
}

public class SidebarCupDto
{
	public string Name { get; set; } = string.Empty;
	public DateTime StartsAtUtc { get; set; }
}
using backend.Features.Sidebar.Dtos;

namespace backend.Features.Sidebar.Services;

public interface ISidebarService
{
	Task<SidebarDto> GetAsync();
}
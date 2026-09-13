namespace backend.Features.Teams.Services;

using backend.Data;
using backend.Entities.Identity;
using backend.Entities.Teams;
using backend.Features.Teams.Dtos;
using Microsoft.EntityFrameworkCore;

public class TeamService : ITeamService
{
	private readonly AppDbContext _context;

	public TeamService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<List<TeamDto>> GetAllAsync(string auth0Id)
	{
		var me = await GetUserAsync(auth0Id);

		var teams = await _context.Teams
			.AsNoTracking()
			.Where(x => !x.IsDeleted)
			.Include(x => x.OwnerUser)
			.Include(x => x.GameClub)
			.Include(x => x.Members.Where(m => !m.IsDeleted))
				.ThenInclude(m => m.User)
			.OrderBy(x => x.Name)
			.ToListAsync();

		return teams.Select(x => Map(x, me.UserId)).ToList();
	}

	public async Task<TeamDto?> GetMineAsync(string auth0Id)
	{
		var me = await GetUserAsync(auth0Id);

		var membership = await _context.TeamMembers
			.AsNoTracking()
			.Where(x => x.UserId == me.UserId && !x.IsDeleted && !x.Team.IsDeleted)
			.Select(x => x.TeamId)
			.FirstOrDefaultAsync();

		if (membership == Guid.Empty)
			return null;

		var team = await LoadTeam(membership);
		return team is null ? null : Map(team, me.UserId);
	}

	public async Task<TeamDto> CreateAsync(string auth0Id, CreateTeamDto dto)
	{
		var name = (dto.Name ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(name))
			throw new InvalidOperationException("Team name is required.");

		var me = await GetUserAsync(auth0Id);

		if (me.GameClubId is null)
			throw new InvalidOperationException("Set your game club in profile before creating a team.");

		var alreadyInTeam = await _context.TeamMembers.AnyAsync(x =>
			x.UserId == me.UserId && !x.IsDeleted && !x.Team.IsDeleted);

		if (alreadyInTeam)
			throw new InvalidOperationException("You already belong to a team. Leave it first.");

		var team = new Team
		{
			TeamId = Guid.NewGuid(),
			Name = name,
			LogoUrl = dto.LogoUrl?.Trim() ?? string.Empty,
			Description = dto.Description?.Trim() ?? string.Empty,
			OwnerUserId = me.UserId,
			GameClubId = me.GameClubId.Value
		};

		_context.Teams.Add(team);
		_context.TeamMembers.Add(new TeamMember
		{
			TeamMemberId = Guid.NewGuid(),
			TeamId = team.TeamId,
			UserId = me.UserId,
			IsCaptain = true,
			JoinedAt = DateTime.UtcNow
		});

		await _context.SaveChangesAsync();

		var created = await LoadTeam(team.TeamId);
		return Map(created!, me.UserId);
	}

	public async Task<TeamDto> AddMemberAsync(string auth0Id, Guid teamId, AddTeamMemberDto dto)
	{
		var me = await GetUserAsync(auth0Id);
		var team = await LoadTeam(teamId) ?? throw new InvalidOperationException("Team not found.");

		if (team.OwnerUserId != me.UserId)
			throw new InvalidOperationException("Only the team owner can add members.");

		if (dto.UserId == me.UserId)
			throw new InvalidOperationException("You are already on this team.");

		var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == dto.UserId && !x.IsDeleted)
			?? throw new InvalidOperationException("User not found.");

		var inAnother = await _context.TeamMembers.AnyAsync(x =>
			x.UserId == user.UserId && !x.IsDeleted && !x.Team.IsDeleted);

		if (inAnother)
			throw new InvalidOperationException("That manager already belongs to a team.");

		_context.TeamMembers.Add(new TeamMember
		{
			TeamMemberId = Guid.NewGuid(),
			TeamId = team.TeamId,
			UserId = user.UserId,
			IsCaptain = false,
			JoinedAt = DateTime.UtcNow
		});

		await _context.SaveChangesAsync();

		var updated = await LoadTeam(team.TeamId);
		return Map(updated!, me.UserId);
	}

	public async Task LeaveOrRemoveAsync(string auth0Id, Guid teamId, Guid userId)
	{
		var me = await GetUserAsync(auth0Id);
		var team = await LoadTeam(teamId) ?? throw new InvalidOperationException("Team not found.");

		var isOwner = team.OwnerUserId == me.UserId;
		if (!isOwner && userId != me.UserId)
			throw new InvalidOperationException("You can only leave your own membership.");

		if (userId == team.OwnerUserId)
			throw new InvalidOperationException("The owner cannot leave. Transfer ownership later or keep the team.");

		var member = await _context.TeamMembers.FirstOrDefaultAsync(x =>
			x.TeamId == teamId && x.UserId == userId && !x.IsDeleted)
			?? throw new InvalidOperationException("Member not found.");

		member.IsDeleted = true;
		member.UpdatedAtUTC = DateTime.UtcNow;
		await _context.SaveChangesAsync();
	}

	private async Task<Team?> LoadTeam(Guid teamId)
	{
		return await _context.Teams
			.Include(x => x.OwnerUser)
			.Include(x => x.GameClub)
			.Include(x => x.Members.Where(m => !m.IsDeleted))
				.ThenInclude(m => m.User)
			.FirstOrDefaultAsync(x => x.TeamId == teamId && !x.IsDeleted);
	}

	private static TeamDto Map(Team team, Guid myUserId)
	{
		return new TeamDto
		{
			TeamId = team.TeamId,
			Name = team.Name,
			LogoUrl = team.LogoUrl,
			Description = team.Description,
			OwnerUserId = team.OwnerUserId,
			OwnerDisplayName = team.OwnerUser.DisplayName,
			GameClubId = team.GameClubId,
			GameClubName = team.GameClub?.Name ?? string.Empty,
			IsMine = team.Members.Any(x => x.UserId == myUserId && !x.IsDeleted),
			IsOwner = team.OwnerUserId == myUserId,
			Members = team.Members
				.Where(x => !x.IsDeleted)
				.OrderByDescending(x => x.IsCaptain)
				.ThenBy(x => x.User.DisplayName)
				.Select(x => new TeamMemberDto
				{
					TeamMemberId = x.TeamMemberId,
					UserId = x.UserId,
					DisplayName = x.User.DisplayName,
					GamerTag = x.User.GamerTag,
					AvatarUrl = x.User.AvatarUrl,
					IsCaptain = x.IsCaptain,
					JoinedAt = x.JoinedAt
				})
				.ToList()
		};
	}

	private async Task<User> GetUserAsync(string auth0Id)
	{
		return await _context.Users.FirstOrDefaultAsync(x => x.Auth0Id == auth0Id && !x.IsDeleted)
			?? throw new UnauthorizedAccessException("User not found.");
	}
}

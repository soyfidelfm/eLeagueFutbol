using backend.Features.Matches.Dtos;

namespace backend.Features.Matches.Services;

public interface IPendingMatchResultService
{
	Task<PendingMatchResultDto> SubmitAsync(Guid matchId, string auth0Id, SubmitMatchResultProposalDto dto);

	Task<PendingMatchResultDto?> GetPendingAsync(Guid matchId, string auth0Id);

	Task<MatchResultDto> AcceptAsync(Guid matchId, Guid proposalId, string auth0Id);

	Task RejectAsync(Guid matchId, Guid proposalId, string auth0Id);
}

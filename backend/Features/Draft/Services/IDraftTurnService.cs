using backend.Entities.Clubs;
using backend.Entities.Draft;
using backend.Features.Draft.Dtos;

public interface IDraftTurnService
{
	Task ProcessDraftTimerAsync(DraftSession session);

	Task<UserClub?> GetCurrentPickOwnerClubAsync(DraftSession session);

	Task AdvanceDraftTurnAsync(
		DraftSession session,
		List<DraftOrder> order);

	Task<DraftCenterDto> BuildDraftCenterAsync(
		Guid seasonId,
		Guid userId,
		DraftSession session);

	Task<List<DraftOrder>> GetDraftOrderAsync(Guid seasonId);

	DraftOrder? GetCurrentTurn(
		DraftSession session,
		List<DraftOrder> order);

	int GetSnakeOrderPosition(
		int currentPickNumber,
		int totalTeams);

	int GetSecondsRemaining(DraftSession session);

	decimal GetDraftPrice(
		int? valueEur,
		int overall);
}
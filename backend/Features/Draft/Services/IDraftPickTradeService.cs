using backend.Features.Draft.Dtos;

public interface IDraftPickTradeService
{
	Task<List<DraftPickAssetDto>> GetMyTradablePicksAsync(
		Guid seasonId,
		string auth0Id);

	Task<DraftPickTradeDto> ProposeTradeAsync(
		Guid seasonId,
		string auth0Id,
		ProposeDraftPickTradeDto dto);

	Task<DraftPickTradeDto> AcceptTradeAsync(
		Guid seasonId,
		string auth0Id,
		Guid tradeId);

	Task RejectTradeAsync(
		Guid seasonId,
		string auth0Id,
		Guid tradeId);

	Task CancelTradeAsync(
		Guid seasonId,
		string auth0Id,
		Guid tradeId);
}
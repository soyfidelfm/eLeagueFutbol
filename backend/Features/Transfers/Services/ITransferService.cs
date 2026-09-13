namespace backend.Features.Transfers.Services;

using backend.Features.Transfers.Dtos;

public interface ITransferService
{
	Task<TransferBoardDto> GetBoardAsync(string auth0Id, Guid seasonId);

	Task<TransferOfferDto> CreateOfferAsync(string auth0Id, Guid seasonId, CreateTransferOfferDto dto);

	Task<TransferOfferDto> AcceptOfferAsync(string auth0Id, Guid transferRecordId);

	Task<TransferOfferDto> RejectOfferAsync(string auth0Id, Guid transferRecordId);

	Task CancelOfferAsync(string auth0Id, Guid transferRecordId);
}

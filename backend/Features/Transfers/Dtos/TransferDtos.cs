namespace backend.Features.Transfers.Dtos;

public class TransferBoardDto
{
	public Guid SeasonId { get; set; }
	public Guid? MyUserClubId { get; set; }
	public string MyClubName { get; set; } = string.Empty;
	public decimal CurrentBudget { get; set; }
	public List<TransferMarketPlayerDto> Market { get; set; } = [];
	public List<TransferOfferDto> IncomingOffers { get; set; } = [];
	public List<TransferOfferDto> OutgoingOffers { get; set; } = [];
	public List<TransferOfferDto> History { get; set; } = [];
}

public class TransferMarketPlayerDto
{
	public Guid GamePlayerId { get; set; }
	public Guid OwnerUserClubId { get; set; }
	public Guid OwnerUserId { get; set; }
	public string OwnerClubName { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Position { get; set; } = string.Empty;
	public int Overall { get; set; }
	public int? MarketValue { get; set; }
	public string? ImageUrl { get; set; }
	public bool HasMyPendingOffer { get; set; }
}

public class TransferOfferDto
{
	public Guid TransferRecordId { get; set; }
	public Guid SeasonId { get; set; }
	public Guid GamePlayerId { get; set; }
	public string PlayerName { get; set; } = string.Empty;
	public string Position { get; set; } = string.Empty;
	public int Overall { get; set; }
	public string? ImageUrl { get; set; }
	public Guid? FromUserClubId { get; set; }
	public string FromClubName { get; set; } = string.Empty;
	public Guid ToUserClubId { get; set; }
	public string ToClubName { get; set; } = string.Empty;
	public decimal TransferFee { get; set; }
	public int? PlayerMarketValue { get; set; }
	public string TransferType { get; set; } = string.Empty;
	public DateTime TransferDateUTC { get; set; }
	public bool IsIncoming { get; set; }
}

public class CreateTransferOfferDto
{
	public Guid GamePlayerId { get; set; }
	public decimal TransferFee { get; set; }
}

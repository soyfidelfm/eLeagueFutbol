namespace backend.Entities.Transfers;

using backend.Entities.Clubs;
using backend.Entities.Common;
using backend.Entities.GameData;
using backend.Entities.Tournaments;

public class TransferRecord : BaseEntity
{
	public Guid TransferRecordId { get; set; }

	public Guid SeasonId { get; set; }

	public Guid GamePlayerId { get; set; }

	public Guid? FromUserClubId { get; set; }

	public Guid ToUserClubId { get; set; }

	// Snapshot del valor del jugador cuando ocurrió la operación
	public int? PlayerMarketValue { get; set; }

	// Lo que realmente se pagó
	public decimal TransferFee { get; set; }

	public DateTime TransferDateUTC { get; set; }

	public string TransferType { get; set; } = string.Empty;
	// Draft
	// Transfer
	// FreeAgent
	// Admin

	public string Notes { get; set; } = string.Empty;

	// Navigation Properties
	public Season Season { get; set; } = null!;

	public GamePlayer GamePlayer { get; set; } = null!;

	public UserClub? FromUserClub { get; set; }

	public UserClub ToUserClub { get; set; } = null!;
}
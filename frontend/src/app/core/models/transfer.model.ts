export interface TransferBoard {
  seasonId: string;
  myUserClubId: string | null;
  myClubName: string;
  currentBudget: number;
  market: TransferMarketPlayer[];
  incomingOffers: TransferOffer[];
  outgoingOffers: TransferOffer[];
  history: TransferOffer[];
}

export interface TransferMarketPlayer {
  gamePlayerId: string;
  ownerUserClubId: string;
  ownerUserId: string;
  ownerClubName: string;
  name: string;
  position: string;
  overall: number;
  marketValue: number | null;
  imageUrl: string | null;
  hasMyPendingOffer: boolean;
}

export interface TransferOffer {
  transferRecordId: string;
  seasonId: string;
  gamePlayerId: string;
  playerName: string;
  position: string;
  overall: number;
  imageUrl: string | null;
  fromUserClubId: string | null;
  fromClubName: string;
  toUserClubId: string;
  toClubName: string;
  transferFee: number;
  playerMarketValue: number | null;
  transferType: string;
  transferDateUTC: string;
  isIncoming: boolean;
}

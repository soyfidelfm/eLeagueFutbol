export interface DraftCenter {
  myUserId: string;
  currentBudget: number;

  state: DraftState;
  draftOrder: DraftOrder[];
  recentPicks: DraftPick[];
  mySquad: MyDraftSquad;
}
export interface DraftState {
  draftSessionId: string;
  isActive: boolean;
  currentRound: number;
  currentPickNumber: number;
  currentUserId?: string;
  currentClubName: string;
  secondsRemaining: number;
  totalTeams: number;
  playersPerTeam: number;
}

export interface DraftOrder {
  pickOrder: number;
  userId: string;
  clubName: string;
  logoUrl?: string;
  isCurrentPick: boolean;
}

export interface DraftPick {
  pickNumber: number;
  roundNumber: number;
  userId: string;
  clubName: string;
  gamePlayerId: string;
  playerName: string;
  position: string;
  overall: number;
  playerImageUrl?: string;
  pickedAtUTC: string;
}

export interface MyDraftSquad {
  totalPlayers: number;
  players: DraftSquadPlayer[];
}

export interface DraftSquadPlayer {
  gamePlayerId: string;
  name: string;
  position: string;
  overall: number;
  imageUrl?: string;
  pickNumber: number;
}

export interface DraftPlayer {
  gamePlayerId: string;

  name: string;
  position: string;

  overall: number;
  potential: number;

  pace: number;
  shooting: number;
  passing: number;
  dribbling: number;
  defending: number;
  physic: number;

  value: number;

  nation: string;
  age: number;

  imageUrl?: string;
  gameClubName: string;
}

export interface MakeDraftPick {
  gamePlayerId: string;
}
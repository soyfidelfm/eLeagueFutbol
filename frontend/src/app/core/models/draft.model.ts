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
  isPaused?: boolean;
  isCompleted?: boolean;
  timerEnabled?: boolean;
  pickSeconds?: number;
  /** Server-authoritative expiry of the current pick (UTC ISO). Null when paused, timer off or inactive. */
  currentPickDeadlineUTC?: string | null;
  /** Server clock when the state was built, used to correct client clock skew. */
  serverTimeUTC?: string;
}

export interface UpdateDraftTimer {
  timerEnabled: boolean;
  pickSeconds: number;
}

export interface DraftSettings {
  defaultPickSeconds: number;
  minPickSeconds: number;
  maxPickSeconds: number;
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

export interface DraftLotteryPreview {
  drawNumber: number;
  isFinal: boolean;
  draftOrder: DraftOrder[];
}
export type DraftPlayerPoolFilter =
  | 'ClubOnly'
  | 'IncludeFreeAgents'
  | 'FreeAgentsOnly';
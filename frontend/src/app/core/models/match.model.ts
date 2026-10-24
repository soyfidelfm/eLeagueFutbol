export type MatchStatus = 'Scheduled' | 'Live' | 'Finished' | 'Cancelled';

export interface MatchTeamStatsDto {
  topScorer: string;
  goals: number;
  topAssist: string;
  assists: number;
  streak: string[];
}

export interface MatchTeamDto {
  userId?: string;
  clubId: string;
  name: string;
  club: string;
  logoUrl: string;
  stats: MatchTeamStatsDto;
}

export interface MatchDto {
  matchId: string;
  weekNumber: number;
  isMatchOfTheWeek: boolean;

  status: MatchStatus;

  arena: string;
  mode: string;

  matchDate: string;
  matchTime: string;

  homeScore?: number;
  awayScore?: number;

  home: MatchTeamDto;
  away: MatchTeamDto;
}

export interface MatchWeekDto {
  value: number;
  label: string;
}

export interface UploadMatchResultDto {
  homeScore: number;
  awayScore: number;
  notes?: string;
  screenshotUrl?: string;
}

export interface MatchResultDto {
  matchId: string;
  homeScore: number;
  awayScore: number;
  status: MatchStatus;
}

export interface Match {
  stage: string;
  score: string;
  status: string;
}

export interface SaveManualMatchStatsRequest {
  homeGoals: number;
  awayGoals: number;

  homePossession: number;
  awayPossession: number;

  homeShots: number;
  awayShots: number;

  homeExpectedGoals: number;
  awayExpectedGoals: number;

  homePasses: number;
  awayPasses: number;

  homeTackles: number;
  awayTackles: number;

  homeTacklesWon: number;
  awayTacklesWon: number;

  homeInterceptions: number;
  awayInterceptions: number;

  homeSaves: number;
  awaySaves: number;

  homeFoulsCommitted: number;
  awayFoulsCommitted: number;

  homeOffsides: number;
  awayOffsides: number;

  homeCorners: number;
  awayCorners: number;

  homeFreeKicks: number;
  awayFreeKicks: number;

  homePenaltyKicks: number;
  awayPenaltyKicks: number;

  homeYellowCards: number;
  awayYellowCards: number;

  homePlayers: ManualPlayerStats[];
  awayPlayers: ManualPlayerStats[];
}

export interface ManualPlayerStats {
  position: string;
  playerName: string;
  rating: number;
  goals: number;
  assists: number;
}


export interface PendingResultPlayer {
  position: string;
  playerName: string;
  rating: number;
  goals: number;
  assists: number;
}

export interface PendingMatchResult {
  pendingMatchResultId: string;
  matchId: string;
  submittedByUserId: string;
  submittedByName: string;
  status: 'Pending' | 'Accepted' | 'Rejected';
  homeScore: number;
  awayScore: number;
  summary?: {
    homePossession: number;
    awayPossession: number;
    homeShots: number;
    awayShots: number;
    homeExpectedGoals: number;
    awayExpectedGoals: number;
    homePasses: number;
    awayPasses: number;
  } | null;
  homePlayers: PendingResultPlayer[];
  awayPlayers: PendingResultPlayer[];
  createdAtUTC: string;
  isMine: boolean;
  canReview: boolean;
}

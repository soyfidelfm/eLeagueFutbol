export type MatchStatsUploadType =
  | 'MatchSummary'
  | 'HomeTeamPerformance'
  | 'AwayTeamPerformance';

export interface MatchStatsUploadResult {
  matchId: string;
  hasMatchSummary: boolean;
  hasHomeTeamPerformance: boolean;
  hasAwayTeamPerformance: boolean;
  isReadyToProcess: boolean;
  status: string;
}

export interface MatchSummaryStatsOcrPreview {
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

  rawLines: string[];
  warnings: string[];
}

export interface MatchPlayerStatsOcrPreview {
  position: string;
  playerName: string;
  rating: number;
  goals: number;
  assists: number;
}

export interface TeamPerformanceOcrPreview {
  players: MatchPlayerStatsOcrPreview[];
  rawLines: string[];
  warnings: string[];
}

export interface ConfirmMatchSummaryRequest {
  matchId: string;

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
}
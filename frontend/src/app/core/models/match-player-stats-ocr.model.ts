// core/models/match-player-stats-ocr.model.ts

export interface MatchPlayerStatsOcrPreview {
  position: string;
  playerName: string;
  rating: number;

  goals: number;
  assists: number;

  shots: number;
  shotAccuracy: number;

  passes: number;
  passAccuracy: number;

  dribbles: number;
  dribbleSuccessRate: number;

  tackles: number;
  tackleSuccessRate: number;

  offsides: number;
  foulsCommitted: number;

  possessionWon: number;
  possessionLost: number;

  minutesPlayed: number;

  distanceCoveredKm: number;
  distanceSprintedKm: number;

  isPlayerOfTheMatch: boolean;
}

export interface ConfirmMatchPlayerStats extends MatchPlayerStatsOcrPreview {
  matchId: string;
  userId: string;
}

export interface MatchOcrEventPreview {
  minute: number;
  type: string;
  playerName: string;
  teamName: string;
  rawText: string;
}

export interface MatchOcrPreview {
  homeTeamName: string;
  awayTeamName: string;

  homeScore?: number | null;
  awayScore?: number | null;

  events: MatchOcrEventPreview[];

  rawLines: string[];
  warnings: string[];
}
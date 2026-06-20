export type MatchStatus = 'Scheduled' | 'Live' | 'Finished' | 'Cancelled';

export interface MatchTeamStatsDto {
  topScorer: string;
  goals: number;
  topAssist: string;
  assists: number;
  streak: string[];
}

export interface MatchTeamDto {
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

export interface UploadMatchResultDto {
  homeScore: number;
  awayScore: number;
}
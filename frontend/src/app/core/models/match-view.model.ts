export interface MatchView {
  id: string;
  weekNumber: number;
  isMatchOfTheWeek: boolean;

  arena: string;
  mode: string;
  date: string;
  time: string;

  homeScore?: number;
  awayScore?: number;

  home: MatchViewTeam;
  away: MatchViewTeam;
}

export interface MatchViewTeam {
  name: string;
  club: string;
  logo: string;
  stats: MatchViewStats;
}

export interface MatchViewStats {
  topScorer: string;
  goals: number;
  topAssist: string;
  assists: number;
  streak: string[];
}
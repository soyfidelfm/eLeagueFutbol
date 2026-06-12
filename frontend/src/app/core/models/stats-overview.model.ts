export interface StatsOverview {
  kpis: StatsKpi[];
  topPlayers: TopPlayer[];
  performance: PerformancePoint[];
  goalsBreakdown: GoalsBreakdown;
}

export interface StatsKpi {
  label: string;
  value: string;
  icon: string;
}

export interface TopPlayer {
  rank: number;
  userId: string;
  gamerTag: string;
  clubName: string;
  avatarUrl: string;
  matches: number;
  goals: number;
  assists: number;
  rating: number;
}

export interface PerformancePoint {
  label: string;
  goals: number;
  assists: number;
  rating: number;
}

export interface GoalsBreakdown {
  totalGoals: number;
  assists: number;
  yellowCards: number;
  redCards: number;
}
export interface SidebarData {
  season: SidebarSeason | null;
  nextMatch?: SidebarNextMatch | null;
  standing?: SidebarStanding | null;
  leagueTopScorer?: SidebarScorer | null;
  teamTopScorer?: SidebarScorer | null;
  leagueTopAssist?: SidebarAssist | null;
}

export interface SidebarNextMatch {
  matchId: string;
  seasonId: string;
  weekNumber: number;
  opponent: string;
  opponentLogoUrl: string;
  isHome: boolean;
  matchDateUtc: string;
  stage: string;
}

export interface SidebarStanding {
  position: number;
  totalClubs: number;
  points: number;
  clubName: string;
  isTopFive: boolean;
}

export interface SidebarScorer {
  playerName: string;
  clubName: string;
  goals: number;
}

export interface SidebarSeason {
  label: string;
  name: string;
  totalClubs: number;
  totalMatches: number;
  startsAtUtc: string | null;
}


export interface SidebarAssist {
  playerName: string;
  clubName: string;
  assists: number;
}

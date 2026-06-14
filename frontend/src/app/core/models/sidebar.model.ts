export interface SidebarData {
  season: SidebarSeason | null;
  cup: SidebarCup | null;
}

export interface SidebarSeason {
  label: string;
  name: string;
  totalClubs: number;
  totalMatches: number;
}

export interface SidebarCup {
  name: string;
  startsAtUtc: string;
}
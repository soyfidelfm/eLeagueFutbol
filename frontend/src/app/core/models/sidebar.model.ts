export interface SidebarData {
  season: SidebarSeason | null;
}

export interface SidebarSeason {
  label: string;
  name: string;
  totalClubs: number;
  totalMatches: number;
  startsAtUtc: string | null;
}

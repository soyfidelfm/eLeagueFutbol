export interface MyClubDto {
  clubId: string;
  name: string;
  abbreviation: string | null;
  logoUrl: string | null;
  description: string | null;
  slogan: string | null;
  region: string | null;
  division: string | null;

  members: number;

  matchesPlayed: number;
  wins: number;
  losses: number;
  draws: number;
  goalsFor: number;
  goalsAgainst: number;
  goalDifference: number;
  rating: number;

  players: MyClubPlayerDto[];
  activities: MyClubActivityDto[];
}

export interface MyClubPlayerDto {
  playerId: string;
  name: string;
  position: string | null;
  overall: number | null;
  avatarUrl: string | null;

  x: number;
  y: number;
}

export interface MyClubActivityDto {
  id: string;
  type: string;
  text: string;
  icon: string;
  createdAtUTC: string;
}
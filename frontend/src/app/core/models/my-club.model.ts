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

  chemistry: number;
  formation: string;

  players: MyClubPlayerDto[];
  activities: MyClubActivityDto[];
}

export interface MyClubPlayerDto {
  playerId: string;
  name: string;
  position: string | null;
  overall: number | null;
  avatarUrl: string | null;

  // NUEVO
  slotIndex: number;
  nation: string | null;
  league: string | null;
  chemistry: number;

  // Para pintar la cancha
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
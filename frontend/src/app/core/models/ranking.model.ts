export interface PlayerRankingDto {
  rank: number;

  userId: string;
  playerName: string;
  avatarUrl: string;

  clubId: string | null;
  clubName: string;
  clubLogoUrl: string;

  points: number;
  wins: number;
  losses: number;

  goalsFor: number;
  goalsAgainst: number;

  winRate: number;

  form: string[];
}

export interface ClubRankingDto {
  rank: number;

  clubId: string;
  clubName: string;
  clubLogoUrl: string;

  points: number;

  wins: number;
  draws: number;
  losses: number;

  goalsFor: number;
  goalsAgainst: number;
  goalDifference: number;

  winRate: number;

  form: string[];
}

export interface RankingsResponseDto {
  seasonId: string;
  seasonName: string;

  playerRankings: PlayerRankingDto[];
  clubRankings: ClubRankingDto[];
}
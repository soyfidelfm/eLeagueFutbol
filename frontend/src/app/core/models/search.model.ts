export interface SearchPlayerResult {
  userId: string;
  displayName: string;
  gamerTag: string;
  avatarUrl: string;
  clubName: string;
  isMe: boolean;
}

export interface SearchClubResult {
  clubId: string;
  seasonId: string;
  name: string;
  logoUrl: string;
  ownerName: string;
  isMine: boolean;
}

export interface SearchMatchResult {
  matchId: string;
  seasonId: string;
  weekNumber: number;
  homeName: string;
  awayName: string;
  homeScore: number;
  awayScore: number;
  status: string;
  matchDate: string;
}

export interface GlobalSearchResult {
  players: SearchPlayerResult[];
  clubs: SearchClubResult[];
  matches: SearchMatchResult[];
}

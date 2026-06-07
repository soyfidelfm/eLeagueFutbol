export interface User {
  userId: string;
  auth0Id: string;
  email: string;
  displayName: string;
  avatarUrl: string;
  timeZone: string;

  clubName: string;
  gameClubId: string | null;
  gameClubLeague: string;
  gamerTag: string;

  lastLoginAtUTC: string | null;
  isActive: boolean;
}

export interface SyncUserDto {
  auth0Id: string;
  email: string;
  displayName: string;
  avatarUrl: string;
  timeZone: string;
}

export interface UpdateUserDto {
  displayName: string;
  avatarUrl: string;
  timeZone: string;

  clubName: string;
  gameClubId: string | null;
  gamerTag: string;
}

export interface MeUserDto {
  userId: string;
  auth0Id: string;
  email: string;
  displayName: string;
  avatarUrl: string;
  timeZone: string;
  gamerTag?: string | null;
  isActive: boolean;
  lastLoginAtUTC?: string | null;
  claims: ClaimDto[];
}

export interface ClaimDto {
  type: string;
  value: string;
}
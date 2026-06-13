export interface User {
  userId: string;
  auth0Id: string;
  email: string;
  displayName: string;
  avatarUrl: string;
  timeZone: string;
  lastLoginAtUTC?: string;
  isActive: boolean;

  clubName: string;
  gameClubId?: string | null;
  gamerTag: string;
  gameClubLeague: string;

  role: string;
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

  clubName?: string | null;
  gameClubId?: string | null;

  role: string;

  claims: ClaimDto[];
}

export interface ClaimDto {
  type: string;
  value: string;
}
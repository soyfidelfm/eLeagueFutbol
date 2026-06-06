export interface User {
  userId: string;
  auth0Id: string;
  email: string;
  displayName: string;
  avatarUrl: string;
  timeZone: string;
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
}
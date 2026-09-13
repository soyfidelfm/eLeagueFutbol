export type SeasonPhase =
  | 'Registration'
  | 'DraftReady'
  | 'Drafting'
  | 'Competition'
  | 'Closed';

export interface Season {
  seasonId: string;

  name: string;
  description: string;

  startDate: string;
  endDate: string;

  isCurrent: boolean;
  isActive: boolean;

  registrationOpen: boolean;
  maxPlayers: number;

  totalWeeks: number;

  phase?: SeasonPhase;
  registeredCount?: number;
  hasDraft?: boolean;
  draftActive?: boolean;
  hasSchedule?: boolean;

  createdAtUTC: string;
  updatedAtUTC?: string | null;
}

export interface CreateSeasonDto {
  name: string;
  description: string;

  startDate: string;
  endDate: string;

  timeZone: string;

  isCurrent: boolean;
  isActive: boolean;

  registrationOpen: boolean;
  maxPlayers: number;

  totalWeeks: number;
}

export interface UpdateSeasonDto {
  name: string;
  description: string;

  startDate: string;
  endDate: string;

  timeZone: string;

  isCurrent: boolean;
  isActive: boolean;

  registrationOpen: boolean;
  maxPlayers: number;

  totalWeeks: number;
}

export interface RegisterSeasonDto {
  gameClubId: string;
}

export interface SeasonRegistration {
  seasonRegistrationId: string;

  seasonId: string;
  userId: string;
  gameClubId: string;

  registeredAtUTC: string;
}

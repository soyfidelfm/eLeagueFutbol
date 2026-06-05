export interface Season {
  seasonId: string;

  name: string;
  description: string;

  startDate: string;
  endDate: string;

  isCurrent: boolean;
  isActive: boolean;

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
}

export interface UpdateSeasonDto {
  name: string;
  description: string;

  startDate: string;
  endDate: string;

  timeZone: string;

  isCurrent: boolean;
  isActive: boolean;
}
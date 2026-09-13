import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from '../services/api.service.ts';
import { API_ENDPOINTS } from '../constants/api-endpoints';

export interface TeamMemberDto {
  teamMemberId: string;
  userId: string;
  displayName: string;
  gamerTag: string;
  avatarUrl: string;
  isCaptain: boolean;
  joinedAt: string;
}

export interface TeamDto {
  teamId: string;
  name: string;
  logoUrl: string;
  description: string;
  ownerUserId: string;
  ownerDisplayName: string;
  gameClubId: string;
  gameClubName: string;
  isMine: boolean;
  isOwner: boolean;
  members: TeamMemberDto[];
}

@Injectable({
  providedIn: 'root'
})
export class TeamApiService extends ApiService {
  constructor(protected override http: HttpClient) {
    super(http);
  }

  getAll(): Observable<TeamDto[]> {
    return this.get<TeamDto[]>(API_ENDPOINTS.teams);
  }

  getMine(): Observable<TeamDto | null> {
    return this.get<TeamDto | null>(`${API_ENDPOINTS.teams}/mine`);
  }

  create(name: string, description = ''): Observable<TeamDto> {
    return this.post<TeamDto>(API_ENDPOINTS.teams, { name, description });
  }

  addMember(teamId: string, userId: string): Observable<TeamDto> {
    return this.post<TeamDto>(`${API_ENDPOINTS.teams}/${teamId}/members`, { userId });
  }

  removeMember(teamId: string, userId: string): Observable<void> {
    return this.delete<void>(`${API_ENDPOINTS.teams}/${teamId}/members/${userId}`);
  }
}

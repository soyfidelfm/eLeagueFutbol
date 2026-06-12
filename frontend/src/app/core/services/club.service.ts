import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from '../services/api.service.ts';
import { API_ENDPOINTS } from '../constants/api-endpoints';

import { MyClubDto } from '../models/my-club.model';

export interface UpdateMyClubLineupPlayerDto {
  playerId: string;
  slotIndex: number;
  position?: string;
}

export interface UpdateMyClubLineupDto {
  formation: string;
  players: UpdateMyClubLineupPlayerDto[];
}

@Injectable({
  providedIn: 'root'
})
export class ClubService extends ApiService {

  constructor(
    protected override http: HttpClient
  ) {
    super(http);
  }

  getMyClub(): Observable<MyClubDto> {
    return this.get<MyClubDto>(
      `${API_ENDPOINTS.clubs}/my`
    );
  }

  updateMyClubLogo(logoUrl: string): Observable<MyClubDto> {
    return this.http.put<MyClubDto>(
      `${API_ENDPOINTS.clubs}/my/logo`,
      { logoUrl }
    );
  }

  updateMyClubLineup(payload: UpdateMyClubLineupDto): Observable<MyClubDto> {
    return this.http.put<MyClubDto>(
      `${API_ENDPOINTS.clubs}/my/lineup`,
      payload
    );
  }
}
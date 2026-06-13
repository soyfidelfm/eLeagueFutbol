import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from '../services/api.service.ts';
import { API_ENDPOINTS } from '../constants/api-endpoints';

import { GameClub } from '../models/game-club.model';

@Injectable({
  providedIn: 'root'
})
export class GameClubService extends ApiService {

  constructor(
    protected override http: HttpClient
  ) {
    super(http);
  }

  getAll(): Observable<GameClub[]> {
    return this.get<GameClub[]>(
      `${API_ENDPOINTS.gameData}/clubs`
    );
  }

  getById(gameClubId: string): Observable<GameClub> {
    return this.get<GameClub>(
      `${API_ENDPOINTS.gameData}/clubs/${gameClubId}`
    );
  }

  getLeagues(): Observable<string[]> {
    return this.get<string[]>(
      `${API_ENDPOINTS.gameData}/clubs/leagues`
    );
  }

  getByLeague(league: string): Observable<GameClub[]> {
    return this.get<GameClub[]>(
      `${API_ENDPOINTS.gameData}/clubs/by-league?league=${encodeURIComponent(league)}`
    );
  }
}
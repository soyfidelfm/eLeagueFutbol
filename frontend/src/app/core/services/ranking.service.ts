import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from '../services/api.service.ts';

import { API_ENDPOINTS } from '../constants/api-endpoints';

import {
  RankingsResponseDto
} from '../models/ranking.model';

@Injectable({
  providedIn: 'root'
})
export class RankingService extends ApiService {

  constructor(
    protected override http: HttpClient
  ) {
    super(http);
  }

  getRankings(seasonId?: string | null): Observable<RankingsResponseDto> {
    let params = new HttpParams();

    if (seasonId) {
      params = params.set('seasonId', seasonId);
    }

    return this.get<RankingsResponseDto>(
      API_ENDPOINTS.rankings,
      params
    );
  }
}
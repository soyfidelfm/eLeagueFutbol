// core/services/match-stats.service.ts

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from '../services/api.service.ts';
import { API_ENDPOINTS } from '../constants/api-endpoints';
import {
  ConfirmMatchPlayerStats,
  MatchPlayerStatsOcrPreview
} from '../models/match-player-stats-ocr.model';

@Injectable({
  providedIn: 'root'
})
export class MatchStatsService extends ApiService {

  constructor(
    protected override http: HttpClient
  ) {
    super(http);
  }

  previewPlayerStats(image: File): Observable<MatchPlayerStatsOcrPreview> {
    const formData = new FormData();
    formData.append('image', image);

    return this.http.post<MatchPlayerStatsOcrPreview>(
      API_ENDPOINTS.matchStatsOcrPreview,
      formData
    );
  }

  confirmPlayerStats(dto: ConfirmMatchPlayerStats): Observable<string> {
    return this.post<string>(
      API_ENDPOINTS.matchStatsConfirmPlayer,
      dto
    );
  }
}
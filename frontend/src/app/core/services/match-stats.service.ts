import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { API_ENDPOINTS } from '../constants/api-endpoints';

export type MatchStatsUploadType =
  | 'MatchSummary'
  | 'HomeTeamPerformance'
  | 'AwayTeamPerformance';

export interface MatchStatsUploadResult {
  matchId: string;
  hasMatchSummary: boolean;
  hasHomeTeamPerformance: boolean;
  hasAwayTeamPerformance: boolean;
  isReadyToProcess: boolean;
  status: string;
}

@Injectable({
  providedIn: 'root'
})
export class MatchStatsService {
  constructor(
    private http: HttpClient
  ) {}

  uploadStatsImage(
    matchId: string,
    type: MatchStatsUploadType,
    image: File
  ): Observable<MatchStatsUploadResult> {
    const formData = new FormData();

    formData.append('type', type);
    formData.append('image', image);

    return this.http.post<MatchStatsUploadResult>(
      API_ENDPOINTS.matchStatsUpload(matchId),
      formData
    );
  }
}
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { API_ENDPOINTS } from '../constants/api-endpoints';

import {
  ConfirmMatchSummaryRequest,
  MatchStatsUploadResult,
  MatchStatsUploadType,
  MatchSummaryStatsOcrPreview,
  TeamPerformanceOcrPreview
} from '../models/match-stats.model';

@Injectable({
  providedIn: 'root'
})
export class MatchStatsService {
  constructor(
    private readonly http: HttpClient
  ) {}

  uploadStatsImage(
    matchId: string,
    type: MatchStatsUploadType,
    image: File
  ): Observable<MatchStatsUploadResult> {
    const formData = new FormData();

    formData.append('type', type);
    formData.append('image', image, image.name);

    return this.http.post<MatchStatsUploadResult>(
      API_ENDPOINTS.matchStatsUpload(matchId),
      formData
    );
  }

  previewMatchSummary(
    image: File
  ): Observable<MatchSummaryStatsOcrPreview> {
    const formData = new FormData();

    formData.append('image', image, image.name);

    return this.http.post<MatchSummaryStatsOcrPreview>(
      API_ENDPOINTS.matchStatsOcrSummaryPreview,
      formData
    );
  }

  previewTeamPerformance(
    image: File
  ): Observable<TeamPerformanceOcrPreview> {
    const formData = new FormData();

    formData.append('image', image, image.name);

    return this.http.post<TeamPerformanceOcrPreview>(
      API_ENDPOINTS.matchStatsOcrTeamPerformancePreview,
      formData
    );
  }

  confirmMatchSummary(
    dto: ConfirmMatchSummaryRequest
  ): Observable<void> {
    return this.http.post<void>(
      API_ENDPOINTS.matchStatsConfirmSummary,
      dto
    );
  }
}
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from '../services/api.service.ts';
import { API_ENDPOINTS } from '../constants/api-endpoints';

import {
  MatchDto,
  MatchWeekDto,
  UploadMatchResultDto,
  MatchResultDto
} from '../models/match.model';

@Injectable({
  providedIn: 'root'
})
export class MatchService extends ApiService {

  constructor(
    protected override http: HttpClient
  ) {
    super(http);
  }

  getWeeks(seasonId: string): Observable<MatchWeekDto[]> {
    const params = new HttpParams()
      .set('seasonId', seasonId);

    return super.get<MatchWeekDto[]>(
      `${API_ENDPOINTS.matches}/weeks`,
      params
    );
  }

  getByWeek(
    seasonId: string,
    weekNumber: number
  ): Observable<MatchDto[]> {
    const params = new HttpParams()
      .set('seasonId', seasonId);

    return super.get<MatchDto[]>(
      `${API_ENDPOINTS.matches}/week/${weekNumber}`,
      params
    );
  }

  getById(matchId: string): Observable<MatchDto> {
    return super.get<MatchDto>(
      `${API_ENDPOINTS.matches}/${matchId}`
    );
  }

  uploadResult(
    matchId: string,
    dto: UploadMatchResultDto
  ): Observable<MatchResultDto> {
    return super.post<MatchResultDto>(
      `${API_ENDPOINTS.matches}/${matchId}/result`,
      dto
    );
  }

  generateSchedule(seasonId: string): Observable<void> {
    return super.post<void>(
      `${API_ENDPOINTS.matches}/generate-schedule/${seasonId}`,
      {}
    );
  }
}
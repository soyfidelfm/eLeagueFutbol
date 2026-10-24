import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from '../services/api.service.ts';
import { API_ENDPOINTS } from '../constants/api-endpoints';
import { getAppTimeZone } from '../i18n/timezone';

import {
  MatchDto,
  MatchWeekDto,
  UploadMatchResultDto,
  MatchResultDto,
  SaveManualMatchStatsRequest,
  PendingMatchResult,

} from '../models/match.model';
import {
  TeamPlayerOption
} from '../models/team-player-option.model';

@Injectable({
  providedIn: 'root'
})
export class MatchService extends ApiService {

  constructor(http: HttpClient) {
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
    // Backend formats matchDate/matchTime in the user's settings time zone.
    const params = new HttpParams()
      .set('seasonId', seasonId)
      .set('timeZone', getAppTimeZone());

    return super.get<MatchDto[]>(
      `${API_ENDPOINTS.matches}/week/${weekNumber}`,
      params
    );
  }

  getById(
    matchId: string
  ): Observable<MatchDto> {
    return super.get<MatchDto>(
      `${API_ENDPOINTS.matches}/${matchId}`,
      new HttpParams().set('timeZone', getAppTimeZone())
    );
  }

  getMatchPlayerOptions(
  matchId: string
): Observable<{
  homeTeamPlayers: TeamPlayerOption[];
  awayTeamPlayers: TeamPlayerOption[];
}> {
  return super.get<{
    homeTeamPlayers: TeamPlayerOption[];
    awayTeamPlayers: TeamPlayerOption[];
  }>(
    `${API_ENDPOINTS.matches}/${matchId}/player-options`
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

  /** Upload the 3 screenshots; OCR runs and the result waits for the opponent's approval. */
  submitResultProposal(
    matchId: string,
    summaryImage: File,
    homePerformanceImage: File,
    awayPerformanceImage: File
  ): Observable<PendingMatchResult> {
    const form = new FormData();
    form.append('SummaryImage', summaryImage);
    form.append('HomePerformanceImage', homePerformanceImage);
    form.append('AwayPerformanceImage', awayPerformanceImage);

    return super.post<PendingMatchResult>(
      `${API_ENDPOINTS.matches}/${matchId}/result-proposals`,
      form
    );
  }

  /** Returns null when the match has no pending proposal (204). */
  getPendingResultProposal(matchId: string): Observable<PendingMatchResult | null> {
    return super.get<PendingMatchResult | null>(
      `${API_ENDPOINTS.matches}/${matchId}/result-proposals/pending`
    );
  }

  acceptResultProposal(matchId: string, proposalId: string): Observable<MatchResultDto> {
    return super.post<MatchResultDto>(
      `${API_ENDPOINTS.matches}/${matchId}/result-proposals/${proposalId}/accept`,
      {}
    );
  }

  rejectResultProposal(matchId: string, proposalId: string): Observable<void> {
    return super.post<void>(
      `${API_ENDPOINTS.matches}/${matchId}/result-proposals/${proposalId}/reject`,
      {}
    );
  }

  generateSchedule(seasonId: string): Observable<void> {
    return super.post<void>(
      `${API_ENDPOINTS.matches}/generate-schedule/${seasonId}`,
      {}
    );
  }

  getManualStats(
    matchId: string
  ): Observable<SaveManualMatchStatsRequest> {
    return super.get<SaveManualMatchStatsRequest>(
      `${API_ENDPOINTS.matches}/${matchId}/manual-stats`
    );
  }

  saveManualStats(
    matchId: string,
    request: SaveManualMatchStatsRequest
  ): Observable<{
    message: string;
    matchId: string;
  }> {
    return super.post<{
      message: string;
      matchId: string;
    }>(
      `${API_ENDPOINTS.matches}/${matchId}/manual-stats`,
      request
    );
  }
}
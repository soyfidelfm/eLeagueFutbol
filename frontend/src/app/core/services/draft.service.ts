import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from '../services/api.service.ts';
import { API_ENDPOINTS } from '../constants/api-endpoints';

import {
  DraftCenter,
  DraftPick,
  DraftPlayer,
  DraftOrder,
  MakeDraftPick
} from '../models/draft.model';

export interface DraftLotteryPreview {
  drawNumber: number;
  isFinal: boolean;
  draftOrder: DraftOrder[];
}

@Injectable({
  providedIn: 'root'
})
export class DraftService extends ApiService {

  constructor(
    protected override http: HttpClient
  ) {
    super(http);
  }

  getCenter(seasonId: string): Observable<DraftCenter> {
    return this.get<DraftCenter>(
      `${API_ENDPOINTS.draft}/center?seasonId=${seasonId}`
    );
  }

  generateDraftOrder(
    seasonId: string,
    userIds: string[]
  ): Observable<DraftCenter> {

    return this.post<DraftCenter>(
      `${API_ENDPOINTS.draft}/generate-order?seasonId=${seasonId}`,
      {
        userIds
      }
    );
  }

  startDraft(
    seasonId: string
  ): Observable<DraftCenter> {

    return this.post<DraftCenter>(
      `${API_ENDPOINTS.draft}/start?seasonId=${seasonId}`,
      {}
    );
  }

  previewDraftOrder(
    seasonId: string,
    drawNumber: number
  ): Observable<DraftLotteryPreview> {

    return this.get<DraftLotteryPreview>(
      `${API_ENDPOINTS.draft}/preview-order?seasonId=${seasonId}&drawNumber=${drawNumber}`
    );
  }

  getPlayers(
    seasonId: string,
    search = '',
    position = '',
    page = 1,
    pageSize = 30
  ): Observable<DraftPlayer[]> {

    const params = new URLSearchParams();

    params.set('seasonId', seasonId);
    params.set('page', page.toString());
    params.set('pageSize', pageSize.toString());

    if (search.trim()) {
      params.set('search', search.trim());
    }

    if (position.trim()) {
      params.set('position', position.trim());
    }

    return this.get<DraftPlayer[]>(
      `${API_ENDPOINTS.draft}/players?${params.toString()}`
    );
  }

  getPicks(seasonId: string): Observable<DraftPick[]> {
    return this.get<DraftPick[]>(
      `${API_ENDPOINTS.draft}/picks?seasonId=${seasonId}`
    );
  }

  makePick(
    seasonId: string,
    gamePlayerId: string
  ): Observable<DraftPick> {

    const body: MakeDraftPick = {
      gamePlayerId
    };

    return this.post<DraftPick>(
      `${API_ENDPOINTS.draft}/picks?seasonId=${seasonId}`,
      body
    );
  }
}
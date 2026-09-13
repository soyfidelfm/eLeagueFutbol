import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from '../services/api.service.ts';
import { API_ENDPOINTS } from '../constants/api-endpoints';
import {
  Season,
  CreateSeasonDto,
  UpdateSeasonDto,
  RegisterSeasonDto,
  SeasonRegistration
} from '../models/season.model';

@Injectable({
  providedIn: 'root'
})
export class SeasonService extends ApiService {

  constructor(
    protected override http: HttpClient
  ) {
    super(http);
  }

  getAll(): Observable<Season[]> {
    return this.get<Season[]>(
      API_ENDPOINTS.seasons
    );
  }

  getById(id: string): Observable<Season> {
    return this.get<Season>(
      `${API_ENDPOINTS.seasons}/${id}`
    );
  }

  getCurrent(): Observable<Season> {
    return this.get<Season>(
      `${API_ENDPOINTS.seasons}/current`
    );
  }

  create(dto: CreateSeasonDto): Observable<Season> {
    return this.post<Season>(
      API_ENDPOINTS.seasons,
      dto
    );
  }

  update(id: string, dto: UpdateSeasonDto): Observable<void> {
    return this.put<void>(
      `${API_ENDPOINTS.seasons}/${id}`,
      dto
    );
  }

  remove(id: string): Observable<void> {
    return super.delete<void>(
      `${API_ENDPOINTS.seasons}/${id}`
    );
  }

  register(
    seasonId: string,
    dto: RegisterSeasonDto
  ): Observable<SeasonRegistration> {
    return super.post<SeasonRegistration>(
      `${API_ENDPOINTS.seasons}/${seasonId}/register`,
      dto
    );
  }

  isRegistered(
    seasonId: string
  ): Observable<boolean> {
    return super.get<boolean>(
      `${API_ENDPOINTS.seasons}/${seasonId}/is-registered`
    );
  }

  closeRegistration(seasonId: string): Observable<Season> {
    return super.post<Season>(
      `${API_ENDPOINTS.seasons}/${seasonId}/close-registration`,
      {}
    );
  }

  openRegistration(seasonId: string): Observable<Season> {
    return super.post<Season>(
      `${API_ENDPOINTS.seasons}/${seasonId}/open-registration`,
      {}
    );
  }

  setCurrent(seasonId: string): Observable<Season> {
    return super.post<Season>(
      `${API_ENDPOINTS.seasons}/${seasonId}/set-current`,
      {}
    );
  }

  closeSeason(seasonId: string): Observable<Season> {
    return super.post<Season>(
      `${API_ENDPOINTS.seasons}/${seasonId}/close`,
      {}
    );
  }

  activateSeason(seasonId: string): Observable<Season> {
    return super.post<Season>(
      `${API_ENDPOINTS.seasons}/${seasonId}/activate`,
      {}
    );
  }
}

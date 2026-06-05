import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from '../services/api.service.ts';
import { API_ENDPOINTS } from '../constants/api-endpoints';

import {
  Season,
  CreateSeasonDto,
  UpdateSeasonDto
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
}
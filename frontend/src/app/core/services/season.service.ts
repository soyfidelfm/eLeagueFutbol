import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { API_ENDPOINTS } from '../constants/api-endpoints';

import {
  Season,
  CreateSeasonDto,
  UpdateSeasonDto
} from '../models/season.model';

@Injectable({
  providedIn: 'root'
})
export class SeasonService {

  constructor(private http: HttpClient) {}

  getAll(): Observable<Season[]> {
    return this.http.get<Season[]>(
      API_ENDPOINTS.seasons
    );
  }

  getById(id: string): Observable<Season> {
    return this.http.get<Season>(
      `${API_ENDPOINTS.seasons}/${id}`
    );
  }

  create(dto: CreateSeasonDto): Observable<Season> {
    return this.http.post<Season>(
      API_ENDPOINTS.seasons,
      dto
    );
  }

  update(id: string, dto: UpdateSeasonDto): Observable<void> {
    return this.http.put<void>(
      `${API_ENDPOINTS.seasons}/${id}`,
      dto
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(
      `${API_ENDPOINTS.seasons}/${id}`
    );
  }
}
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from './api.service.ts';
import { API_ENDPOINTS } from '../constants/api-endpoints';
import { GlobalSearchResult } from '../models/search.model';

@Injectable({
  providedIn: 'root'
})
export class SearchService extends ApiService {

  constructor(protected override http: HttpClient) {
    super(http);
  }

  /** GET /api/search?q= — up to 5 players, clubs and matches. */
  search(query: string): Observable<GlobalSearchResult> {
    const params = new HttpParams().set('q', query.trim());
    return this.get<GlobalSearchResult>(API_ENDPOINTS.search, params);
  }
}

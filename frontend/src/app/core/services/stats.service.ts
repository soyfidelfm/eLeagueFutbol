import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from '../services/api.service.ts';
import { API_ENDPOINTS } from '../constants/api-endpoints';

import { StatsOverview } from '../models/stats-overview.model';

@Injectable({
  providedIn: 'root'
})
export class StatsService extends ApiService {

  constructor(
    protected override http: HttpClient
  ) {
    super(http);
  }

  getOverview(): Observable<StatsOverview> {

    const timeZone =
      Intl.DateTimeFormat()
        .resolvedOptions()
        .timeZone;

    const params = new HttpParams()
      .set('timeZone', timeZone);

    return this.get<StatsOverview>(
      API_ENDPOINTS.statsOverview,
      params
    );
  }
}
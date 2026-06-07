import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from '../services/api.service.ts';

import { API_ENDPOINTS } from '../constants/api-endpoints';

import { DashboardResponse } from '../models/dashboard-response.model';



@Injectable({
  providedIn: 'root'
})
export class DashboardService extends ApiService {

  constructor(
    protected override http: HttpClient
  ) {
    super(http);
  }

  getDashboard(): Observable<DashboardResponse> {

    const timeZone =
      Intl.DateTimeFormat()
        .resolvedOptions()
        .timeZone;

    const params = new HttpParams()
      .set(
        'timeZone',
        timeZone
      );

    return this.get<DashboardResponse>(
      API_ENDPOINTS.dashboard,
      params
    );
  }
}
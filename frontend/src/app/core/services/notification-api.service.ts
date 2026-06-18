// src/app/core/services/notification-api.service.ts

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from '../services/api.service.ts';
import { API_ENDPOINTS } from '../constants/api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class NotificationApiService extends ApiService {

  constructor(
    protected override http: HttpClient
  ) {
    super(http);
  }

  saveToken(token: string): Observable<void> {
    return this.post<void>(
      `${API_ENDPOINTS.notifications}/token`,
      {
        token,
        deviceType: 'web'
      }
    );
  }
}
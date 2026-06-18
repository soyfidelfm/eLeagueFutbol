import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from '../services/api.service.ts';
import { API_ENDPOINTS } from '../constants/api-endpoints';

import {
  Notification,
  UnreadNotificationCount
} from '../models/notification.model';

export interface SaveNotificationTokenDto {
  token: string;
  deviceType: string;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationInboxService extends ApiService {

  constructor(
    protected override http: HttpClient
  ) {
    super(http);
  }

  saveToken(payload: SaveNotificationTokenDto): Observable<void> {
    return this.http.post<void>(
      `${API_ENDPOINTS.notifications}/token`,
      payload
    );
  }

  getNotifications(): Observable<Notification[]> {
    return this.get<Notification[]>(
      API_ENDPOINTS.notifications
    );
  }

  getUnreadCount(): Observable<UnreadNotificationCount> {
    return this.get<UnreadNotificationCount>(
      `${API_ENDPOINTS.notifications}/unread-count`
    );
  }

  markAsRead(notificationId: string): Observable<void> {
    return this.http.put<void>(
      `${API_ENDPOINTS.notifications}/${notificationId}/read`,
      {}
    );
  }

  markAllAsRead(): Observable<void> {
    return this.http.put<void>(
      `${API_ENDPOINTS.notifications}/read-all`,
      {}
    );
  }
}
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
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

  /** Newest first. `before` is the createdAtUTC of the last item already loaded. */
  getNotifications(options: { unreadOnly?: boolean; before?: string | null; take?: number } = {}): Observable<Notification[]> {
    let params = new HttpParams();

    if (options.unreadOnly) params = params.set('unreadOnly', 'true');
    if (options.before) params = params.set('before', options.before);
    if (options.take) params = params.set('take', String(options.take));

    return this.get<Notification[]>(
      API_ENDPOINTS.notifications,
      params
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
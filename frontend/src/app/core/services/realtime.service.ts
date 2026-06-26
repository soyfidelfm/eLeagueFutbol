// core/services/realtime.service.ts

import { Injectable, NgZone } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';

import { API_ENDPOINTS } from '../constants/api-endpoints';
import {
  RealtimeDraftCompleted,
  RealtimeDraftPickMade,
  RealtimeDraftTurnStarted,
  RealtimeMessageReceived,
  RealtimeNotificationRead,
  RealtimeNotificationReceived
} from '../models/realtime.model';

@Injectable({
  providedIn: 'root'
})
export class RealtimeService {
  private connection?: signalR.HubConnection;
  private isStarting = false;

  readonly messageReceived$ = new Subject<RealtimeMessageReceived>();
  readonly notificationReceived$ = new Subject<RealtimeNotificationReceived>();
  readonly notificationRead$ = new Subject<RealtimeNotificationRead>();
  readonly allNotificationsRead$ = new Subject<{ unreadCount: number }>();

  readonly draftTurnStarted$ = new Subject<RealtimeDraftTurnStarted>();
  readonly draftPickMade$ = new Subject<RealtimeDraftPickMade>();
  readonly draftCompleted$ = new Subject<RealtimeDraftCompleted>();

  constructor(private zone: NgZone) { }

  async start(accessToken: string): Promise<void> {
    if (!accessToken) {
      return;
    }

    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    if (this.isStarting) {
      return;
    }

    this.isStarting = true;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(API_ENDPOINTS.realtimeHub, {
        accessTokenFactory: () => accessToken
      })
      .withAutomaticReconnect()
      .build();

    this.registerEvents();

    try {
      await this.connection.start();
      console.log('Realtime connected');
    } catch (error) {
      console.error('Realtime connection failed:', error);
    } finally {
      this.isStarting = false;
    }
  }

  async stop(): Promise<void> {
    if (!this.connection) {
      return;
    }

    await this.connection.stop();
    this.connection = undefined;
    this.isStarting = false;
  }

  get isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }

  private registerEvents(): void {
    if (!this.connection) {
      return;
    }

    this.connection.off('MessageReceived');
    this.connection.off('NotificationReceived');
    this.connection.off('NotificationRead');
    this.connection.off('AllNotificationsRead');
    this.connection.off('DraftTurnStarted');
    this.connection.off('DraftPickMade');
    this.connection.off('DraftCompleted');

    this.connection.on('MessageReceived', (payload: RealtimeMessageReceived) => {
      this.zone.run(() => this.messageReceived$.next(payload));
    });

    this.connection.on('NotificationReceived', (payload: RealtimeNotificationReceived) => {
      this.zone.run(() => this.notificationReceived$.next(payload));
    });

    this.connection.on('NotificationRead', (payload: RealtimeNotificationRead) => {
      this.zone.run(() => this.notificationRead$.next(payload));
    });

    this.connection.on('AllNotificationsRead', (payload: { unreadCount: number }) => {
      this.zone.run(() => this.allNotificationsRead$.next(payload));
    });

    this.connection.on('DraftTurnStarted', (payload: RealtimeDraftTurnStarted) => {
      this.zone.run(() => this.draftTurnStarted$.next(payload));
    });

    this.connection.on('DraftPickMade', (payload: RealtimeDraftPickMade) => {
      this.zone.run(() => this.draftPickMade$.next(payload));
    });

    this.connection.on('DraftCompleted', (payload: RealtimeDraftCompleted) => {
      this.zone.run(() => this.draftCompleted$.next(payload));
    });
  }
}
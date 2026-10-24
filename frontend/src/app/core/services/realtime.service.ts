import { Injectable, OnDestroy } from '@angular/core';
import { AuthService } from '@auth0/auth0-angular';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { BehaviorSubject, Subject } from 'rxjs';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { MessageDto, TypingEvent } from '../models/message.model';
import { Notification } from '../models/notification.model';
import {
  DraftRealtimeEvent,
  MessageReadEvent,
  PresenceUser
} from '../models/presence.model';

@Injectable({
  providedIn: 'root'
})
export class RealtimeService implements OnDestroy {
  private connection?: HubConnection;
  private starting?: Promise<void>;
  private currentUserId = '';
  private lastSeasonId: string | null = null;

  private readonly onlineUsersSubject = new BehaviorSubject<PresenceUser[]>([]);
  readonly onlineUsers$ = this.onlineUsersSubject.asObservable();

  private readonly messageSubject = new Subject<MessageDto>();
  readonly messages$ = this.messageSubject.asObservable();

  private readonly messageReadSubject = new Subject<MessageReadEvent>();
  readonly messageRead$ = this.messageReadSubject.asObservable();

  private readonly notificationSubject = new Subject<Notification>();
  readonly notifications$ = this.notificationSubject.asObservable();

  private readonly userOnlineSubject = new Subject<PresenceUser>();
  readonly userOnline$ = this.userOnlineSubject.asObservable();

  private readonly userOfflineSubject = new Subject<PresenceUser>();
  readonly userOffline$ = this.userOfflineSubject.asObservable();

  private readonly draftEventSubject = new Subject<DraftRealtimeEvent>();
  readonly draftEvents$ = this.draftEventSubject.asObservable();

  private readonly typingSubject = new Subject<TypingEvent>();
  readonly typing$ = this.typingSubject.asObservable();

  constructor(private auth: AuthService) {}

  /** Tells the receiver that the current user is typing. The server resolves the sender from the token. */
  async sendTyping(receiverUserId: string): Promise<void> {
    if (!receiverUserId || !this.connection || this.connection.state !== HubConnectionState.Connected) {
      return;
    }

    try {
      await this.connection.invoke('Typing', receiverUserId);
    } catch (err) {
      console.error('Failed to send typing event:', err);
    }
  }

  setCurrentUserId(userId: string): void {
    this.currentUserId = userId;
  }

  isOnline(userId: string): boolean {
    return this.onlineUsersSubject.value.some(x => x.userId === userId);
  }

  async connect(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return;
    }

    if (this.starting) {
      return this.starting;
    }

    this.starting = this.startConnection();

    try {
      await this.starting;
    } finally {
      this.starting = undefined;
    }
  }

  async joinSeason(seasonId: string): Promise<void> {
    if (seasonId) {
      this.lastSeasonId = seasonId;
    }

    if (!seasonId || !this.connection || this.connection.state !== HubConnectionState.Connected) {
      return;
    }

    await this.connection.invoke('JoinSeason', seasonId);
  }

  async leaveSeason(seasonId: string): Promise<void> {
    if (seasonId && this.lastSeasonId === seasonId) {
      this.lastSeasonId = null;
    }

    if (!seasonId || !this.connection || this.connection.state !== HubConnectionState.Connected) {
      return;
    }

    await this.connection.invoke('LeaveSeason', seasonId);
  }

  async disconnect(): Promise<void> {
    if (!this.connection) {
      return;
    }

    await this.connection.stop();
  }

  ngOnDestroy(): void {
    void this.disconnect();
  }

  private async startConnection(): Promise<void> {
    this.connection = new HubConnectionBuilder()
      .withUrl(`${environment.api.baseUrl}/hubs/app`, {
        accessTokenFactory: async () => {
          try {
            return await firstValueFrom(this.auth.getAccessTokenSilently());
          } catch {
            return '';
          }
        },
        withCredentials: false
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('ReceiveMessage', (message: MessageDto) => {
      if (this.currentUserId) {
        message.isMine = message.senderUserId === this.currentUserId;
      }

      this.messageSubject.next(message);

      window.dispatchEvent(
        new CustomEvent('eleague-message-received', { detail: message })
      );
    });

    this.connection.on('MessageRead', (event: MessageReadEvent) => {
      this.messageReadSubject.next(event);
    });

    this.connection.on('NotificationCreated', (notification: Notification) => {
      this.notificationSubject.next(notification);

      window.dispatchEvent(
        new CustomEvent('eleague-notification-received', { detail: notification })
      );
    });

    this.connection.on('UserOnline', (user: PresenceUser) => {
      this.upsertOnlineUser(user);

      if (user.userId !== this.currentUserId) {
        this.userOnlineSubject.next(user);
      }
    });

    this.connection.on('UserOffline', (user: PresenceUser) => {
      this.removeOnlineUser(user.userId);
      this.userOfflineSubject.next(user);
    });

    this.connection.on('OnlineUsers', (users: PresenceUser[]) => {
      this.onlineUsersSubject.next(users ?? []);
    });

    this.connection.on('UserTyping', (event: TypingEvent) => {
      this.typingSubject.next(event);
    });

    this.connection.on('DraftEvent', (event: DraftRealtimeEvent) => {
      this.draftEventSubject.next(event);
      window.dispatchEvent(
        new CustomEvent('eleague-draft-event', { detail: event })
      );
    });

    this.connection.onreconnected(async () => {
      if (!this.lastSeasonId || !this.connection) {
        return;
      }

      try {
        await this.connection.invoke('JoinSeason', this.lastSeasonId);
      } catch (err) {
        console.error('Failed to rejoin season after reconnect:', err);
      }
    });

    await this.connection.start();
  }

  private upsertOnlineUser(user: PresenceUser): void {
    const users = this.onlineUsersSubject.value.filter(x => x.userId !== user.userId);
    this.onlineUsersSubject.next([...users, user]);
  }

  private removeOnlineUser(userId: string): void {
    this.onlineUsersSubject.next(
      this.onlineUsersSubject.value.filter(x => x.userId !== userId)
    );
  }
}

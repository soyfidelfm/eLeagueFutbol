import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, Subject, catchError, firstValueFrom, of, tap } from 'rxjs';

import { NotificationInboxService } from './notification-inbox.service';
import { RealtimeService } from './realtime.service';
import { Notification as AppNotification } from '../models/notification.model';
import { TranslationService } from '../i18n/translation.service';

/**
 * Shared state for the navbar bell and the /notifications page:
 * unread badge count, live SignalR arrivals and read actions.
 */
@Injectable({ providedIn: 'root' })
export class NotificationCenterService {
  private readonly api = inject(NotificationInboxService);
  private readonly realtime = inject(RealtimeService);
  private readonly router = inject(Router);
  private readonly i18n = inject(TranslationService);

  private readonly unreadCountSubject = new BehaviorSubject<number>(0);
  readonly unreadCount$ = this.unreadCountSubject.asObservable();

  /** New notifications pushed over SignalR (NotificationCreated). */
  private readonly incomingSubject = new Subject<AppNotification>();
  readonly incoming$ = this.incomingSubject.asObservable();

  /** Emits an id when one item is read, or 'all' after mark-all, so other lists can sync. */
  private readonly readSubject = new Subject<string | 'all'>();
  readonly read$ = this.readSubject.asObservable();

  constructor() {
    this.realtime.notifications$.subscribe(notification => {
      if (!notification?.notificationId) return;
      this.unreadCountSubject.next(this.unreadCountSubject.value + (notification.isRead ? 0 : 1));
      this.incomingSubject.next(notification);
    });
  }

  get unreadCount(): number {
    return this.unreadCountSubject.value;
  }

  refreshUnreadCount(): void {
    this.api.getUnreadCount()
      .pipe(catchError(() => of({ count: this.unreadCount })))
      .subscribe(result => this.unreadCountSubject.next(result?.count ?? 0));
  }

  list(options: { unreadOnly?: boolean; before?: string | null; take?: number }): Observable<AppNotification[]> {
    return this.api.getNotifications(options);
  }

  markRead(notification: AppNotification): Observable<void> {
    if (notification.isRead) return of(undefined);

    return this.api.markAsRead(notification.notificationId).pipe(
      tap(() => {
        notification.isRead = true;
        this.unreadCountSubject.next(Math.max(this.unreadCount - 1, 0));
        this.readSubject.next(notification.notificationId);
      })
    );
  }

  markAllRead(): Observable<void> {
    return this.api.markAllAsRead().pipe(
      tap(() => {
        this.unreadCountSubject.next(0);
        this.readSubject.next('all');
      })
    );
  }

  /** Marks as read first (best effort), then follows the notification's URL. */
  async open(notification: AppNotification): Promise<void> {
    try {
      await firstValueFrom(this.markRead(notification), { defaultValue: undefined });
    } catch {
      // still navigate
    }

    const url = (notification.url ?? '').trim();
    if (!url) return;

    if (url.startsWith('/')) {
      void this.router.navigateByUrl(url);
      return;
    }

    try {
      const parsed = new URL(url);
      if (parsed.origin === window.location.origin) {
        void this.router.navigateByUrl(parsed.pathname + parsed.search + parsed.hash);
      } else {
        window.open(url, '_blank', 'noopener');
      }
    } catch {
      void this.router.navigateByUrl('/' + url);
    }
  }

  iconFor(type: string | null | undefined): string {
    const t = (type ?? '').toLowerCase();

    if (t.startsWith('match')) return 'fa-solid fa-futbol';
    if (t.startsWith('draft')) return 'fa-solid fa-list-ol';
    if (t.startsWith('transfer')) return 'fa-solid fa-right-left';
    if (t.startsWith('message')) return 'fa-solid fa-comment-dots';
    if (t.startsWith('season')) return 'fa-solid fa-trophy';
    if (t === 'system') return 'fa-solid fa-bullhorn';
    return 'fa-solid fa-bell';
  }

  /** "just now", "5 min ago", "3 h ago", "2 d ago"; null when older than a week (use a date). */
  relativeTime(value: string | null | undefined): string | null {
    if (!value) return null;
    const iso = /T\d{2}:\d{2}/.test(value) && !/(Z|[+-]\d{2}:?\d{2})$/.test(value) ? `${value}Z` : value;
    const diff = Date.now() - new Date(iso).getTime();
    if (isNaN(diff)) return null;

    const minutes = Math.floor(diff / 60000);
    if (minutes < 1) return this.i18n.t('just now');
    if (minutes < 60) return this.i18n.t('{n} min ago', { n: minutes });
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return this.i18n.t('{n} h ago', { n: hours });
    const days = Math.floor(hours / 24);
    if (days < 7) return this.i18n.t('{n} d ago', { n: days });
    return null;
  }
}

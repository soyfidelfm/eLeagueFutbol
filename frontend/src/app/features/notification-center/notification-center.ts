import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription, finalize } from 'rxjs';

import { NotificationCenterService } from '../../core/services/notification-center.service';
import { Notification as AppNotification } from '../../core/models/notification.model';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LocalizedDatePipe } from '../../core/i18n/localized-date.pipe';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { AppLoading } from '../../shared/components/app-loading/app-loading';

type Filter = 'all' | 'unread';

@Component({
  selector: 'app-notification-center',
  standalone: true,
  imports: [CommonModule, TranslatePipe, LocalizedDatePipe, EmptyState, AppLoading],
  templateUrl: './notification-center.html',
  styleUrl: './notification-center.scss'
})
export class NotificationCenter implements OnInit, OnDestroy {
  private static readonly PAGE_SIZE = 20;

  readonly center = inject(NotificationCenterService);
  private readonly cdr = inject(ChangeDetectorRef);

  filter: Filter = 'all';
  items: AppNotification[] = [];
  unreadCount = 0;

  isLoading = true;
  isLoadingMore = false;
  hasMore = false;
  errorMessage = '';

  private requestId = 0;
  private subscription = new Subscription();

  ngOnInit(): void {
    this.center.refreshUnreadCount();

    this.subscription.add(this.center.unreadCount$.subscribe(count => {
      this.unreadCount = count;
      this.cdr.detectChanges();
    }));

    // Live arrivals always land at the top (they are unread, so they fit both filters).
    this.subscription.add(this.center.incoming$.subscribe(notification => {
      if (this.items.some(x => x.notificationId === notification.notificationId)) return;
      this.items = [notification, ...this.items];
      this.cdr.detectChanges();
    }));

    this.subscription.add(this.center.read$.subscribe(id => {
      if (this.filter === 'unread') {
        this.items = id === 'all' ? [] : this.items.filter(x => x.notificationId !== id);
      } else {
        this.items = this.items.map(x => id === 'all' || x.notificationId === id ? { ...x, isRead: true } : x);
      }
      this.cdr.detectChanges();
    }));

    this.load(true);
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  setFilter(filter: Filter): void {
    if (this.filter === filter) return;
    this.filter = filter;
    this.load(true);
  }

  retry(): void {
    this.load(true);
  }

  loadMore(): void {
    if (!this.hasMore || this.isLoadingMore || this.isLoading) return;
    this.load(false);
  }

  private load(reset: boolean): void {
    const id = ++this.requestId;
    const before = reset ? null : this.items[this.items.length - 1]?.createdAtUTC ?? null;

    if (reset) {
      this.isLoading = true;
      this.errorMessage = '';
    } else {
      this.isLoadingMore = true;
    }

    this.center.list({ unreadOnly: this.filter === 'unread', before, take: NotificationCenter.PAGE_SIZE })
      .pipe(finalize(() => {
        if (id !== this.requestId) return;
        this.isLoading = false;
        this.isLoadingMore = false;
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: page => {
          if (id !== this.requestId) return; // a newer filter/request won
          const rows = page ?? [];
          this.items = reset
            ? rows
            : [...this.items, ...rows.filter(n => !this.items.some(x => x.notificationId === n.notificationId))];
          this.hasMore = (page ?? []).length === NotificationCenter.PAGE_SIZE;
        },
        error: () => {
          if (id !== this.requestId) return;
          this.errorMessage = 'Could not load notifications.';
        }
      });
  }

  open(item: AppNotification): void {
    void this.center.open(item);
  }

  markRead(item: AppNotification, event: Event): void {
    event.stopPropagation();
    this.center.markRead(item).subscribe({ error: () => {} });
  }

  markAllRead(): void {
    this.center.markAllRead().subscribe({
      error: () => {
        this.errorMessage = 'Could not mark notifications as read.';
        this.cdr.detectChanges();
      }
    });
  }
}

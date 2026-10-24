import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize, Subscription } from 'rxjs';

import { NotificationCenterService } from '../../core/services/notification-center.service';
import { Notification as AppNotification } from '../../core/models/notification.model';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LocalizedDatePipe } from '../../core/i18n/localized-date.pipe';
import { EmptyState } from '../../shared/components/empty-state/empty-state';

/** Navbar bell: unread badge + dropdown with the latest few notifications. */
@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, RouterLink,
    TranslatePipe, LocalizedDatePipe, EmptyState
  ],
  templateUrl: './notifications.html',
  styleUrl: './notifications.scss'
})
export class Notifications implements OnInit, OnDestroy {
  private static readonly LATEST = 5;

  notifications: AppNotification[] = [];

  unreadCount = 0;
  isOpen = false;
  isLoading = false;

  private subscription = new Subscription();

  constructor(
    readonly center: NotificationCenterService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.center.refreshUnreadCount();

    this.subscription.add(
      this.center.unreadCount$.subscribe(count => {
        this.unreadCount = count;
        this.cdr.detectChanges();
      })
    );

    this.subscription.add(
      this.center.incoming$.subscribe(notification => {
        if (this.notifications.some(x => x.notificationId === notification.notificationId)) return;
        this.notifications = [notification, ...this.notifications].slice(0, Notifications.LATEST);
        this.cdr.detectChanges();
      })
    );

    this.subscription.add(
      this.center.read$.subscribe(id => {
        this.notifications = this.notifications.map(item =>
          id === 'all' || item.notificationId === id ? { ...item, isRead: true } : item);
        this.cdr.detectChanges();
      })
    );
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  toggleDropdown(): void {
    this.isOpen = !this.isOpen;
    this.cdr.detectChanges();

    if (this.isOpen) {
      this.loadLatest();
    }
  }

  closeDropdown(): void {
    this.isOpen = false;
    this.cdr.detectChanges();
  }

  private loadLatest(): void {
    this.isLoading = true;
    this.center.refreshUnreadCount();

    this.subscription.add(
      this.center.list({ take: Notifications.LATEST })
        .pipe(finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        }))
        .subscribe({
          next: items => this.notifications = items ?? [],
          error: () => this.notifications = []
        })
    );
  }

  openNotification(notification: AppNotification): void {
    this.isOpen = false;
    this.cdr.detectChanges();
    void this.center.open(notification);
  }

  markAllAsRead(event: MouseEvent): void {
    event.stopPropagation();
    this.center.markAllRead().subscribe({
      error: error => console.error('Error marking all notifications as read:', error)
    });
  }
}

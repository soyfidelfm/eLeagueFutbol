import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { finalize, Subscription } from 'rxjs';

import { NotificationInboxService } from '../../core/services/notification-inbox.service';
import { Notification as AppNotification } from '../../core/models/notification.model';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LocalizedDatePipe } from '../../core/i18n/localized-date.pipe';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule,
    TranslatePipe, LocalizedDatePipe
  ],
  templateUrl: './notifications.html',
  styleUrl: './notifications.scss'
})
export class Notifications implements OnInit, OnDestroy {
  notifications: AppNotification[] = [];

  unreadCount = 0;
  isOpen = false;
  isLoading = false;

  private notificationListener?: EventListener;
  private subscription = new Subscription();

  constructor(
    private notificationInboxService: NotificationInboxService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadNotifications();

    this.notificationListener = () => {
      this.loadNotifications();
      this.cdr.detectChanges();
    };

    window.addEventListener(
      'eleague-notification-received',
      this.notificationListener
    );
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();

    if (this.notificationListener) {
      window.removeEventListener(
        'eleague-notification-received',
        this.notificationListener
      );
    }
  }

  toggleDropdown(): void {
    this.isOpen = !this.isOpen;
    this.cdr.detectChanges();

    if (this.isOpen) {
      this.loadNotifications();
    }
  }

  closeDropdown(): void {
    this.isOpen = false;
    this.cdr.detectChanges();
  }

  loadNotifications(): void {
    this.isLoading = true;
    this.cdr.detectChanges();

    const notificationsSub = this.notificationInboxService
      .getNotifications()
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: response => {
          this.notifications = response ?? [];
          this.cdr.detectChanges();
        },
        error: error => {
          console.error('Error loading notifications:', error);
          this.notifications = [];
          this.cdr.detectChanges();
        }
      });

    const countSub = this.notificationInboxService
      .getUnreadCount()
      .subscribe({
        next: response => {
          this.unreadCount = response?.count ?? 0;
          this.cdr.detectChanges();
        },
        error: error => {
          console.error('Error loading unread count:', error);
          this.unreadCount = 0;
          this.cdr.detectChanges();
        }
      });

    this.subscription.add(notificationsSub);
    this.subscription.add(countSub);
  }

  openNotification(notification: AppNotification): void {
    if (!notification.isRead) {
      this.notificationInboxService
        .markAsRead(notification.notificationId)
        .subscribe({
          next: () => {
            notification.isRead = true;
            this.unreadCount = Math.max(this.unreadCount - 1, 0);
            this.cdr.detectChanges();
          },
          error: error => {
            console.error('Error marking notification as read:', error);
          }
        });
    }

    this.isOpen = false;
    this.cdr.detectChanges();

    if (notification.url) {
      this.router.navigateByUrl(notification.url);
    }
  }

  markAllAsRead(event: MouseEvent): void {
    event.stopPropagation();

    this.notificationInboxService
      .markAllAsRead()
      .subscribe({
        next: () => {
          this.notifications = this.notifications.map(item => ({
            ...item,
            isRead: true
          }));

          this.unreadCount = 0;
          this.cdr.detectChanges();
        },
        error: error => {
          console.error('Error marking all notifications as read:', error);
        }
      });
  }

  getIcon(type: string): string {
    switch (type) {
      case 'message':
        return 'fa-solid fa-message';

      case 'draft':
        return 'fa-solid fa-users-gear';

      case 'match':
        return 'fa-solid fa-futbol';

      case 'season':
        return 'fa-solid fa-trophy';

      case 'transfer':
        return 'fa-solid fa-right-left';

      default:
        return 'fa-solid fa-bell';
    }
  }
}
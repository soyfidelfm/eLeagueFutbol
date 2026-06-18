import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { finalize, Subscription } from 'rxjs';

import { NotificationInboxService } from '../../core/services/notification-inbox.service';
import { Notification as AppNotification } from '../../core/models/notification.model';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule],
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
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadNotifications();

    this.notificationListener = () => {
      this.loadNotifications();
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

    if (this.isOpen) {
      this.loadNotifications();
    }
  }

  closeDropdown(): void {
    this.isOpen = false;
  }

  loadNotifications(): void {
    this.isLoading = true;

    const notificationsSub = this.notificationInboxService
      .getNotifications()
      .pipe(
        finalize(() => {
          this.isLoading = false;
        })
      )
      .subscribe({
        next: response => {
          this.notifications = response;
        },
        error: error => {
          console.error('Error loading notifications:', error);
        }
      });

    const countSub = this.notificationInboxService
      .getUnreadCount()
      .subscribe({
        next: response => {
          this.unreadCount = response.count;
        },
        error: error => {
          console.error('Error loading unread count:', error);
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
          }
        });
    }

    this.isOpen = false;

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
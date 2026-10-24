import { Component, HostListener, OnDestroy, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { firstValueFrom, Subscription, take } from 'rxjs';

import { Profile } from '../../features/profile/profile';
import { Notifications } from '../../features/notifications/notifications';
import { MessageNotifications } from '../../features/message-notifications/message-notifications';
import { GlobalSearch } from '../../features/global-search/global-search';

import { UserService } from '../../core/services/user.service';
import { SeasonService } from '../../core/services/season.service';
import { MessageService } from '../../core/services/message.service';
import { NotificationService } from '../../core/services/notification.service';
import { NotificationInboxService } from '../../core/services/notification-inbox.service';
import { RealtimeService } from '../../core/services/realtime.service';
import { SettingsService } from '../../core/services/settings.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';

import { MeUserDto } from '../../core/models/user.model';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    Profile,
    Notifications,
    MessageNotifications,
    GlobalSearch,
    TranslatePipe
  ],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss'
})
export class Navbar implements OnInit, OnDestroy {
  me: MeUserDto | null = null;
  private firstProfileCheckDone = false;
  private subscription = new Subscription();

  showToast = false;
  toastMessage = '';

  showUserMenu = false;
  showProfileModal = false;

  unreadMessagesCount = 0;

  avatarLoadFailed = false;
  private lastAvatarUrl = '';

  constructor(
    public auth: AuthService,
    private userService: UserService,
    private seasonService: SeasonService,
    private messageService: MessageService,
    private pushNotificationService: NotificationService,
    private notificationInboxService: NotificationInboxService,
    private realtime: RealtimeService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private settingsService: SettingsService,
    private i18n: TranslationService
  ) {}

  ngOnInit(): void {
    this.loadMe();

    // Reconcile language / time zone / toast preference with the backend after login.
    this.settingsService.load().subscribe({ error: () => {} });
    this.loadUnreadMessagesCount();
    this.setupPushNotifications();

    window.addEventListener('eleague-message-received', () => {
      this.loadUnreadMessagesCount();
    });

    this.subscription.add(
      this.realtime.messages$.subscribe(() => {
        this.loadUnreadMessagesCount();
      })
    );

    this.subscription.add(
      this.realtime.userOnline$.subscribe(user => {
        if (!this.settingsService.toastsEnabled) {
          return;
        }

        const onlineMessage = this.i18n.t('{name} is online', { name: user.displayName });
        this.toastMessage = onlineMessage;
        this.showToast = true;
        this.cdr.detectChanges();

        setTimeout(() => {
          if (this.toastMessage === onlineMessage) {
            this.showToast = false;
            this.cdr.detectChanges();
          }
        }, 3500);
      })
    );

    // New inbox notifications: the center/badge always update; the toast respects the user's setting.
    this.subscription.add(
      this.realtime.notifications$.subscribe(notification => {
        if (!notification?.title || !this.settingsService.toastsEnabled) {
          return;
        }

        const message = notification.title;
        this.toastMessage = message;
        this.showToast = true;
        this.cdr.detectChanges();

        setTimeout(() => {
          if (this.toastMessage === message) {
            this.showToast = false;
            this.cdr.detectChanges();
          }
        }, 3500);
      })
    );

    this.subscription.add(
      this.realtime.draftEvents$.subscribe(event => {
        if (!event?.eventType || !this.settingsService.toastsEnabled) {
          return;
        }

        this.toastMessage = this.realtimeToastMessage(event.eventType);
        this.showToast = true;
        this.cdr.detectChanges();

        setTimeout(() => {
          this.showToast = false;
          this.cdr.detectChanges();
        }, 3500);
      })
    );
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  private realtimeToastMessage(eventType: string): string {
    const type = (eventType ?? '').toLowerCase();

    if (type === 'transferoffer') {
      return 'New transfer offer';
    }

    if (type === 'transfercompleted' || type === 'transferaccepted') {
      return 'Transfer completed';
    }

    if (type === 'transferrejected') {
      return 'Transfer offer rejected';
    }

    if (type === 'transfercancelled') {
      return 'Transfer offer cancelled';
    }

    if (type === 'matchresult' || type.startsWith('match')) {
      return 'Match result updated';
    }

    if (type === 'pickmade' || type === 'pick') {
      return 'New draft pick';
    }

    if (type === 'started' || type === 'start') {
      return 'The draft has started';
    }

    if (type === 'turn' || type === 'turnchanged' || type === 'advanced') {
      return 'Draft turn changed';
    }

    if (type === 'skipped' || type === 'skip') {
      return 'Turn skipped (time expired)';
    }

    if (type === 'completed' || type === 'complete') {
      return 'Draft completed';
    }

    if (type.startsWith('transfer')) {
      return 'Transfer market updated';
    }

    return 'Draft updated';
  }

  async setupPushNotifications(): Promise<void> {
    try {
      const token =
        await this.pushNotificationService.requestPermissionAndGetToken();

      if (!token) {
        return;
      }

      await firstValueFrom(
        this.notificationInboxService.saveToken({
          token,
          deviceType: 'web'
        })
      );

      this.pushNotificationService.listenForMessages();
    } catch (error) {
      console.error('Error setting up push notifications:', error);
    }
  }

  loadMe(): void {
    this.userService.getMe()
      .pipe(take(1))
      .subscribe({
        next: me => {
          const url = (me?.avatarUrl ?? '').trim();
          if (url !== this.lastAvatarUrl) {
            this.avatarLoadFailed = false;
            this.lastAvatarUrl = url;
          }
          this.me = me;

          if (me?.userId) {
            this.realtime.setCurrentUserId(me.userId);
          }

          // Incomplete profiles are handled by the onboarding modal (features/onboarding).
          this.firstProfileCheckDone = true;

          this.cdr.detectChanges();
        },
        error: err => {
          console.error('Error loading me', err);

          this.firstProfileCheckDone = true;
        }
      });
  }

  loadUnreadMessagesCount(): void {
    this.messageService.getConversations()
      .pipe(take(1))
      .subscribe({
        next: conversations => {
          this.unreadMessagesCount = (conversations ?? []).reduce(
            (total, item) => total + item.unreadCount,
            0
          );

          this.cdr.detectChanges();
        },
        error: err => {
          console.error('Error loading unread messages count', err);
          this.unreadMessagesCount = 0;
          this.cdr.detectChanges();
        }
      });
  }

  openDraft(): void {
    this.seasonService.getAll()
      .pipe(take(1))
      .subscribe({
        next: seasons => {
          const currentSeason = (seasons ?? []).find(
            x => x.isCurrent && x.isActive
          );

          if (!currentSeason?.seasonId) {
            this.toastMessage = 'No active season found.';
            this.showToast = true;
            this.cdr.detectChanges();
            return;
          }

          this.router.navigate(['/draft'], {
            queryParams: {
              seasonId: currentSeason.seasonId
            }
          });
        },
        error: err => {
          console.error('Error loading current season', err);
          this.toastMessage = 'Could not open draft.';
          this.showToast = true;
          this.cdr.detectChanges();
        }
      });
  }

  get showAvatarImage(): boolean {
    return !!this.me?.avatarUrl?.trim() && !this.avatarLoadFailed;
  }

  get avatarInitials(): string {
    const source = (this.me?.displayName || this.me?.gamerTag || '').trim();

    if (!source) {
      return '?';
    }

    const parts = source.split(/\s+/).filter(Boolean);
    const initials = parts.length > 1
      ? parts[0][0] + parts[parts.length - 1][0]
      : source.slice(0, 2);

    return initials.toUpperCase();
  }

  onAvatarError(): void {
    this.avatarLoadFailed = true;
    this.cdr.detectChanges();
  }

  toggleUserMenu(event: Event): void {
    event.stopPropagation();
    this.showUserMenu = !this.showUserMenu;
  }

  openProfileModal(event?: MouseEvent): void {
    event?.stopPropagation();
    this.showUserMenu = false;
    this.showProfileModal = true;
  }

  closeProfileModal(): void {
    this.showProfileModal = false;
  }

  logout(): void {
    void this.realtime.disconnect();

    this.auth.logout({
      logoutParams: {
        returnTo: window.location.origin
      }
    });
  }

  onProfileSaved(): void {
    this.showProfileModal = false;
    this.firstProfileCheckDone = true;

    this.loadMe();

    this.toastMessage = 'Profile updated successfully.';
    this.showToast = true;

    setTimeout(() => {
      this.showToast = false;
      this.cdr.detectChanges();
    }, 3000);
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    this.showUserMenu = false;
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.showUserMenu = false;
  }
}

import { Component, HostListener, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { take } from 'rxjs';

import { Profile } from '../../features/profile/profile';
import { Notifications } from '../../features/notifications/notifications';

import { UserService } from '../../core/services/user.service';
import { SeasonService } from '../../core/services/season.service';
import { MessageService } from '../../core/services/message.service';

import { MeUserDto } from '../../core/models/user.model';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    Profile,
    Notifications
  ],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss'
})
export class Navbar implements OnInit {
  me: MeUserDto | null = null;
  private firstProfileCheckDone = false;

  showToast = false;
  toastMessage = '';

  showUserMenu = false;
  showProfileModal = false;

  unreadMessagesCount = 0;

  constructor(
    public auth: AuthService,
    private userService: UserService,
    private seasonService: SeasonService,
    private messageService: MessageService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadMe();
    this.loadUnreadMessagesCount();

    window.addEventListener('eleague-message-received', () => {
      this.loadUnreadMessagesCount();
    });
  }

  loadMe(): void {
    this.userService.getMe()
      .pipe(take(1))
      .subscribe({
        next: me => {
          this.me = me;

          const profileIncomplete =
            !me.gamerTag?.trim() ||
            !me.clubName?.trim() ||
            !me.gameClubId;

          if (!this.firstProfileCheckDone && profileIncomplete) {
            this.showProfileModal = true;
            this.firstProfileCheckDone = true;
          }

          this.cdr.detectChanges();
        },
        error: err => {
          console.error('Error loading me', err);

          if (err.status === 404 && !this.firstProfileCheckDone) {
            this.showProfileModal = true;
            this.firstProfileCheckDone = true;
            this.cdr.detectChanges();
          }
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

  toggleUserMenu(event: MouseEvent): void {
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
}
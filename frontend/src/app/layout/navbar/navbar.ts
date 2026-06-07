import { Component, HostListener, OnInit,ChangeDetectorRef  } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';

import { Profile } from '../../features/profile/profile';
import { UserService } from '../../core/services/user.service';
import { MeUserDto } from '../../core/models/user.model';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    Profile
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

  constructor(
    public auth: AuthService,
    private userService: UserService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.loadMe();
  }

  loadMe(): void {
  this.userService.getMe().subscribe({
    next: me => {
      this.me = me;

      if (
        !this.firstProfileCheckDone &&
        !me.gamerTag?.trim()
      ) {
        this.showProfileModal = true;
        this.firstProfileCheckDone = true;
      }

      this.cdr.detectChanges();
    },
    error: err => {
      console.error('Error loading me', err);
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
        returnTo: `${window.location.origin}/login`
      }
    });
  }

  onProfileSaved(): void {
    this.showProfileModal = false;

    this.loadMe();

    this.toastMessage = 'Profile updated successfully.';
    this.showToast = true;

    setTimeout(() => {
      this.showToast = false;
    }, 3000);
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    this.showUserMenu = false;
  }
}
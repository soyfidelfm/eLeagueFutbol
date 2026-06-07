import { Component, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';

import { Profile } from '../../features/profile/profile';

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
export class Navbar {
showToast = false;
toastMessage = '';

  showUserMenu = false;

  showProfileModal = false;

  constructor(
    public auth: AuthService
  ) { }

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
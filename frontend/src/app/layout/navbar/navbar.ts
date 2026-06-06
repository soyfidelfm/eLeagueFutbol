import { Component, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule
  ],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss'
})
export class Navbar {

  showUserMenu = false;

  showProfileModal = false;

  constructor(
    public auth: AuthService
  ) { }

  toggleUserMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.showUserMenu = !this.showUserMenu;
  }

  openProfileModal(): void {
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

  @HostListener('document:click')
  onDocumentClick(): void {
    this.showUserMenu = false;
  }
}
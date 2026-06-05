import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '@auth0/auth0-angular';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  showArenaIntro = false;
  showExploreModal = false;
  showArenaWord = false;

  constructor(
    public auth: AuthService,
    private router: Router
  ) {
    this.auth.isAuthenticated$.subscribe(isAuthenticated => {
      if (isAuthenticated) {
        this.router.navigateByUrl('/dashboard');
      }
    });
  }

  loginWithProvider(provider: string): void {
    this.auth.loginWithRedirect({
      appState: {
        target: '/dashboard'
      },
      authorizationParams: {
        connection: provider,
        redirect_uri: window.location.origin
      }
    });
  }

  enterArena(): void {

    this.showArenaIntro = true;

    this.showArenaWord = false;

    // show ENTER THE first
    setTimeout(() => {
      this.showArenaWord = true;
    }, 3000);

    // remove overlay
    setTimeout(() => {

      this.showArenaIntro = false;

      const loginCard = document.querySelector('.login-card');

      loginCard?.classList.add('login-card-active');

      setTimeout(() => {
        loginCard?.classList.remove('login-card-active');
      }, 1200);

    }, 2200);

  }

  openExploreModal(): void {
    this.showExploreModal = true;
  }

  closeExploreModal(): void {
    this.showExploreModal = false;
  }

  startFromExplore(): void {
    this.showExploreModal = false;
    this.enterArena();
  }
}
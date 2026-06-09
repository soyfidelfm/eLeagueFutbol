import { Component, ElementRef, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '@auth0/auth0-angular';
import { Router } from '@angular/router';
import { Subject, filter, switchMap, take, takeUntil } from 'rxjs';

import { UserService } from '../../../core/services/user.service';
import { SyncUserDto } from '../../../core/models/user.model';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login implements OnDestroy {
  showArenaIntro = false;
  showExploreModal = false;
  showArenaWord = false;

  private destroy$ = new Subject<void>();
  @ViewChild('loginCard') loginCard!: ElementRef<HTMLDivElement>;

  constructor(
    public auth: AuthService,
    private router: Router,
    private userService: UserService
  ) {
    this.auth.isAuthenticated$
      .pipe(
        filter(isAuthenticated => isAuthenticated),
        take(1),
        switchMap(() => this.auth.user$.pipe(take(1))),
        switchMap(profile => {
          const dto: SyncUserDto = {
            auth0Id: profile?.sub ?? '',
            email:
              profile?.email ??
              profile?.['https://thekiddycloud.com/email'] ??
              '',
            displayName:
              profile?.name ??
              profile?.['https://thekiddycloud.com/name'] ??
              profile?.nickname ??
              profile?.email ??
              '',
            avatarUrl: profile?.picture ?? '',
            timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone
          };

          return this.userService.sync(dto);
        }),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: user => {
          localStorage.setItem('userId', user.userId);
          localStorage.setItem('auth0Id', user.auth0Id);
          localStorage.setItem('email', user.email ?? '');
          localStorage.setItem('displayName', user.displayName ?? '');
          localStorage.setItem('avatarUrl', user.avatarUrl ?? '');
          localStorage.setItem('timeZone', user.timeZone ?? 'UTC');

          localStorage.setItem('clubName', user.clubName ?? '');
          localStorage.setItem('gamerTag', user.gamerTag ?? '');
          localStorage.setItem('gameClubId', user.gameClubId ?? '');
          localStorage.setItem('gameClubLeague', user.gameClubLeague ?? '');

          this.router.navigateByUrl('/dashboard');
        },
        error: error => {
          console.error('Error syncing user:', error);
        }
      });
  }

  loginWithEmail(): void {
    this.auth.loginWithRedirect({
      appState: {
        target: '/dashboard'
      },
      authorizationParams: {
        connection: 'Username-Password-Authentication',
        redirect_uri: window.location.origin,
        audience: 'https://eleaguefutbol-api',
        scope: 'openid profile email offline_access'
      }
    });
  }

  signUpWithEmail(): void {
    this.auth.loginWithRedirect({
      appState: {
        target: '/dashboard'
      },
      authorizationParams: {
        screen_hint: 'signup',
        connection: 'Username-Password-Authentication',
        redirect_uri: window.location.origin,
        audience: 'https://eleaguefutbol-api',
        scope: 'openid profile email offline_access'
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
        redirect_uri: window.location.origin,
        audience: 'https://eleaguefutbol-api',
        scope: 'openid profile email offline_access'
      }
    });
  }

  loginWithGoogle(): void {
    this.loginWithProvider('google-oauth2');
  }

  loginWithMicrosoft(): void {
    this.loginWithProvider('windowslive');
  }

  loginWithApple(): void {
    this.loginWithProvider('apple');
  }

  enterArena(): void {
  this.showArenaIntro = true;
  this.showArenaWord = false;

  setTimeout(() => {
    this.showArenaWord = true;
  }, 3000);

  setTimeout(() => {
    this.showArenaIntro = false;

    const loginCard = document.querySelector('.login-card');

    loginCard?.classList.add('login-card-active');

    // SOLO EN MÓVIL
    if (window.innerWidth <= 768) {
      setTimeout(() => {
        loginCard?.scrollIntoView({
          behavior: 'smooth',
          block: 'start'
        });
      }, 100);
    }

    setTimeout(() => {
      loginCard?.classList.remove('login-card-active');
    }, 1200);

  }, 1200);
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

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { catchError, finalize, of, take } from 'rxjs';

import { UserService } from '../../core/services/user.service';
import { SeasonService } from '../../core/services/season.service';
import { MeUserDto } from '../../core/models/user.model';
import { Season } from '../../core/models/season.model';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Profile } from '../profile/profile';

type OnboardingStep = 'profile' | 'season' | null;

/**
 * Guided onboarding shown inside the authenticated Shell (never on /login):
 *  1) profile incomplete (gamerTag, clubName or gameClubId missing) -> complete profile (reuses <app-profile>)
 *  2) profile complete but not registered in the current open season -> register (reuses POST seasons/{id}/register)
 * Dismissals are stored per user in localStorage:
 *  - eleague.onboarding.<userId>.profile = 'dismissed'
 *  - eleague.onboarding.<userId>.season  = <seasonId dismissed>  (a new season re-shows the invite)
 */
@Component({
  selector: 'app-onboarding',
  standalone: true,
  imports: [CommonModule, TranslatePipe, Profile],
  templateUrl: './onboarding.html',
  styleUrl: './onboarding.scss'
})
export class Onboarding implements OnInit {
  private readonly userService = inject(UserService);
  private readonly seasonService = inject(SeasonService);
  private readonly cdr = inject(ChangeDetectorRef);

  step: OnboardingStep = null;
  me: MeUserDto | null = null;
  season: Season | null = null;

  isRegistering = false;
  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    this.evaluate();
  }

  get profileComplete(): boolean {
    return Onboarding.isProfileComplete(this.me);
  }

  static isProfileComplete(me: MeUserDto | null): boolean {
    return !!me && !!me.gamerTag?.trim() && !!me.clubName?.trim() && !!me.gameClubId;
  }

  private evaluate(): void {
    this.userService.getMe()
      .pipe(take(1), catchError(() => of(null)))
      .subscribe(me => {
        this.me = me;

        if (!Onboarding.isProfileComplete(me)) {
          this.step = this.read('profile') === 'dismissed' ? null : 'profile';
          this.cdr.detectChanges();
          return;
        }

        this.checkSeason();
      });
  }

  private checkSeason(): void {
    this.seasonService.getCurrent()
      .pipe(take(1), catchError(() => of(null)))
      .subscribe(season => {
        if (!season?.seasonId || !season.isActive || !season.registrationOpen || this.read('season') === season.seasonId) {
          this.close();
          return;
        }

        this.seasonService.isRegistered(season.seasonId)
          .pipe(take(1), catchError(() => of(true)))
          .subscribe(registered => {
            this.season = season;
            this.step = registered ? null : 'season';
            this.cdr.detectChanges();
          });
      });
  }

  onProfileSaved(): void {
    // Re-read the profile; continue to the season step once it is complete.
    this.userService.getMe()
      .pipe(take(1), catchError(() => of(null)))
      .subscribe(me => {
        this.me = me;

        if (Onboarding.isProfileComplete(me)) {
          this.checkSeason();
        } else {
          this.cdr.detectChanges();
        }
      });
  }

  register(): void {
    if (!this.season?.seasonId || !this.me?.gameClubId || this.isRegistering) return;

    this.isRegistering = true;
    this.errorMessage = '';

    this.seasonService.register(this.season.seasonId, { gameClubId: this.me.gameClubId })
      .pipe(
        take(1),
        finalize(() => {
          this.isRegistering = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: () => {
          this.successMessage = 'You are registered. Good luck this season!';
          this.cdr.detectChanges();
          setTimeout(() => this.close(), 1800);
        },
        error: err => {
          this.errorMessage = err?.error?.message || 'Could not register.';
        }
      });
  }

  dismiss(): void {
    if (this.step === 'profile') this.write('profile', 'dismissed');
    if (this.step === 'season' && this.season?.seasonId) this.write('season', this.season.seasonId);
    this.close();
  }

  private close(): void {
    this.step = null;
    this.cdr.detectChanges();
  }

  private key(part: string): string {
    const userId = this.me?.userId || localStorage.getItem('userId') || 'unknown';
    return `eleague.onboarding.${userId}.${part}`;
  }

  private read(part: string): string | null {
    try { return localStorage.getItem(this.key(part)); } catch { return null; }
  }

  private write(part: string, value: string): void {
    try { localStorage.setItem(this.key(part), value); } catch { /* ignore */ }
  }
}

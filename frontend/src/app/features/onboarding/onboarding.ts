import { ChangeDetectorRef, Component, HostListener, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { catchError, finalize, of, take } from 'rxjs';

import { UserService } from '../../core/services/user.service';
import { SeasonService } from '../../core/services/season.service';
import { MeUserDto } from '../../core/models/user.model';
import { Season } from '../../core/models/season.model';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Profile } from '../profile/profile';

export interface OnboardingSlide {
  key: 'welcome' | 'profile' | 'season' | 'draft' | 'transfers' | 'results' | 'messages';
  eyebrow: string;
  title: string;
  bullets: string[];
  icon: string;
  accent: string;
  /** Small floating icons around the illustration. */
  chips: string[];
}

/**
 * Product tour carousel + slim reminder banner, mounted in the authenticated Shell.
 *
 * Tour: shown after login unless the user ticked "Don't show this again"
 *   (localStorage eleague.onboarding.<userId>.hidden = '1'). Closing without the box ticked
 *   shows it again on the next login / refresh.
 * Reminder banner: independent of the tour; shown while the profile is incomplete or the user
 *   is not registered in the current open season. Dismissing it only hides it for this session
 *   (sessionStorage eleague.onboarding.<userId>.reminder), so the task is never lost.
 */
@Component({
  selector: 'app-onboarding',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe, Profile],
  templateUrl: './onboarding.html',
  styleUrl: './onboarding.scss'
})
export class Onboarding implements OnInit {
  private readonly userService = inject(UserService);
  private readonly seasonService = inject(SeasonService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly slides: OnboardingSlide[] = [
    {
      key: 'welcome', eyebrow: 'WELCOME', title: 'Welcome to eLeague', icon: 'fa-solid fa-trophy', accent: 'green', chips: ['fa-solid fa-futbol', 'fa-solid fa-ranking-star', 'fa-solid fa-gamepad'],
      bullets: [
        'A competitive FC league: register your club, draft a squad and play weekly matches.',
        'Results feed live standings, stats and rankings for every season.',
        'This quick tour shows you how everything works.'
      ]
    },
    {
      key: 'profile', eyebrow: 'STEP 1', title: 'Complete your profile', icon: 'fa-solid fa-id-card', accent: 'cyan', chips: ['fa-solid fa-user', 'fa-solid fa-shield-halved', 'fa-solid fa-hashtag'],
      bullets: [
        'Add your PS ID / Gamertag so opponents can find you in-game.',
        'Set your team name and pick your in-game club (league + team).',
        'You can edit it anytime from your avatar menu → Profile.'
      ]
    },
    {
      key: 'season', eyebrow: 'STEP 2', title: 'Register for the season', icon: 'fa-solid fa-flag-checkered', accent: 'green', chips: ['fa-solid fa-calendar-check', 'fa-solid fa-users', 'fa-solid fa-check'],
      bullets: [
        'When registration is open, join the current season from the dashboard.',
        'Your profile must be complete before you can register.',
        'After registration closes, admins run the draft lottery and generate the schedule.'
      ]
    },
    {
      key: 'draft', eyebrow: 'DRAFT', title: 'How the Draft works', icon: 'fa-solid fa-list-ol', accent: 'purple', chips: ['fa-solid fa-stopwatch', 'fa-solid fa-coins', 'fa-solid fa-shuffle'],
      bullets: [
        'A lottery sets the order, then it runs as a snake draft: the order reverses every round.',
        'Each pick has a timer; if it runs out, your turn is skipped.',
        'Every player has a value that comes out of your budget. Filter by club players or free agents.'
      ]
    },
    {
      key: 'transfers', eyebrow: 'MARKET', title: 'Transfers & squad', icon: 'fa-solid fa-right-left', accent: 'cyan', chips: ['fa-solid fa-handshake', 'fa-solid fa-euro-sign', 'fa-solid fa-people-group'],
      bullets: [
        'Make offers for other clubs\' players from the Transfer Market; they can accept or reject.',
        'You can cancel your pending offers. Accepted fees move between club budgets.',
        'Set your formation and starting XI from Clubs. Transfers lock when the season closes.'
      ]
    },
    {
      key: 'results', eyebrow: 'MATCHES', title: 'Uploading match results', icon: 'fa-solid fa-camera', accent: 'green', chips: ['fa-solid fa-image', 'fa-solid fa-wand-magic-sparkles', 'fa-solid fa-circle-check'],
      bullets: [
        'After a match, upload 3 screenshots: Match Summary, Home and Away Team Performance.',
        'OCR reads the score and stats into a proposal; your opponent (or an admin) accepts or rejects it.',
        'Once accepted, the match is final and standings update live. You can also enter stats manually.'
      ]
    },
    {
      key: 'messages', eyebrow: 'STAY CONNECTED', title: 'Messages & notifications', icon: 'fa-solid fa-comments', accent: 'purple', chips: ['fa-solid fa-bell', 'fa-solid fa-paper-plane', 'fa-solid fa-circle'],
      bullets: [
        'Chat privately with any player and see when they\'re online or typing.',
        'Get notified about draft turns, transfer offers and match results.',
        'Turn push notifications and in-app alerts on or off in Settings.'
      ]
    }
  ];

  me: MeUserDto | null = null;
  season: Season | null = null;
  isRegistered = true;

  showTour = false;
  index = 0;
  direction: 'next' | 'prev' = 'next';
  dontShowAgain = false;

  showProfileModal = false;
  reminderDismissed = false;

  isRegistering = false;
  registerError = '';
  registerSuccess = '';

  private touchStartX: number | null = null;

  ngOnInit(): void {
    this.userService.getMe()
      .pipe(take(1), catchError(() => of(null)))
      .subscribe(me => {
        this.me = me;
        this.showTour = this.storage(localStorage, 'hidden') !== '1';
        this.reminderDismissed = this.storage(sessionStorage, 'reminder') === '1';
        this.loadSeasonState();
        this.cdr.detectChanges();
      });
  }

  // ---------- state ----------

  get profileComplete(): boolean {
    const me = this.me;
    return !!me && !!me.gamerTag?.trim() && !!me.clubName?.trim() && !!me.gameClubId;
  }

  get canRegister(): boolean {
    return !!this.season?.seasonId && this.season.isActive && this.season.registrationOpen && !this.isRegistered;
  }

  /** What the reminder banner / last-slide CTA should push next. */
  get pendingTask(): 'profile' | 'register' | null {
    if (!this.profileComplete) return 'profile';
    if (this.canRegister) return 'register';
    return null;
  }

  get showReminder(): boolean {
    return !this.showTour && !this.showProfileModal && !this.reminderDismissed && !!this.pendingTask;
  }

  get isLast(): boolean {
    return this.index === this.slides.length - 1;
  }

  private loadSeasonState(): void {
    this.seasonService.getCurrent()
      .pipe(take(1), catchError(() => of(null)))
      .subscribe(season => {
        this.season = season;

        if (!season?.seasonId) {
          this.cdr.detectChanges();
          return;
        }

        this.seasonService.isRegistered(season.seasonId)
          .pipe(take(1), catchError(() => of(true)))
          .subscribe(registered => {
            this.isRegistered = registered;
            this.cdr.detectChanges();
          });
      });
  }

  // ---------- carousel ----------

  goTo(i: number): void {
    if (i < 0 || i >= this.slides.length || i === this.index) return;
    this.direction = i > this.index ? 'next' : 'prev';
    this.index = i;
  }

  next(): void {
    this.goTo(this.index + 1);
  }

  prev(): void {
    this.goTo(this.index - 1);
  }

  closeTour(): void {
    if (this.dontShowAgain) {
      this.setStorage(localStorage, 'hidden', '1');
    }
    this.showTour = false;
  }

  @HostListener('document:keydown', ['$event'])
  onKey(event: KeyboardEvent): void {
    if (this.showProfileModal) {
      if (event.key === 'Escape') this.showProfileModal = false;
      return;
    }

    if (!this.showTour) return;

    if (event.key === 'ArrowRight') this.next();
    else if (event.key === 'ArrowLeft') this.prev();
    else if (event.key === 'Escape') this.closeTour();
  }

  onTouchStart(event: TouchEvent): void {
    this.touchStartX = event.touches[0]?.clientX ?? null;
  }

  onTouchEnd(event: TouchEvent): void {
    if (this.touchStartX === null) return;
    const dx = (event.changedTouches[0]?.clientX ?? this.touchStartX) - this.touchStartX;
    this.touchStartX = null;
    if (Math.abs(dx) < 50) return;
    if (dx < 0) this.next();
    else this.prev();
  }

  // ---------- CTAs ----------

  /** Last-slide primary button. */
  primaryCta(): void {
    const task = this.pendingTask;
    this.closeTour();

    if (task === 'profile') this.openProfile();
    else if (task === 'register') this.register();
  }

  openProfile(): void {
    this.showProfileModal = true;
  }

  onProfileSaved(): void {
    this.userService.getMe()
      .pipe(take(1), catchError(() => of(this.me)))
      .subscribe(me => {
        this.me = me;
        if (this.profileComplete) this.showProfileModal = false;
        this.cdr.detectChanges();
      });
  }

  register(): void {
    if (!this.canRegister || !this.me?.gameClubId || this.isRegistering) return;

    this.isRegistering = true;
    this.registerError = '';

    this.seasonService.register(this.season!.seasonId, { gameClubId: this.me.gameClubId })
      .pipe(
        take(1),
        finalize(() => {
          this.isRegistering = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: () => {
          this.isRegistered = true;
          this.registerSuccess = 'You are registered. Good luck this season!';
          this.reminderDismissed = false;
          this.cdr.detectChanges();
          setTimeout(() => {
            this.registerSuccess = '';
            this.cdr.detectChanges();
          }, 3500);
        },
        error: err => {
          this.registerError = err?.error?.message || 'Could not register.';
          this.reminderDismissed = false;
        }
      });
  }

  dismissReminder(): void {
    this.reminderDismissed = true;
    this.setStorage(sessionStorage, 'reminder', '1');
  }

  // ---------- storage ----------

  private key(part: string): string {
    const userId = this.me?.userId || localStorage.getItem('userId') || 'unknown';
    return `eleague.onboarding.${userId}.${part}`;
  }

  private storage(store: Storage, part: string): string | null {
    try { return store.getItem(this.key(part)); } catch { return null; }
  }

  private setStorage(store: Storage, part: string, value: string): void {
    try { store.setItem(this.key(part), value); } catch { /* storage unavailable */ }
  }
}

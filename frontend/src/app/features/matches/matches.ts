import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subscription, finalize, forkJoin, take } from 'rxjs';

import { MatchService } from '../../core/services/match.service';
import { SeasonService } from '../../core/services/season.service';
import { MatchStatsService } from '../../core/services/match-stats.service';
import { UserService } from '../../core/services/user.service';
import { RealtimeService } from '../../core/services/realtime.service';

import { AppLoading } from '../../shared/components/app-loading/app-loading';

import {
  MatchDto,
  MatchWeekDto
} from '../../core/models/match.model';

import { MatchView } from '../../core/models/match-view.model';

import {
  MatchStatsUploadResult,
  MatchSummaryStatsOcrPreview,
  TeamPerformanceOcrPreview
} from '../../core/models/match-stats.model';

import {
  NeonDropdownComponent,
  NeonDropdownOption
} from '../../shared/components/neon-dropdown/neon-dropdown';

import {
  ManualMatchStatsModal
} from './components/manual-match-stats-modal/manual-match-stats-modal';

@Component({
  selector: 'app-matches',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    NeonDropdownComponent,
    AppLoading,
    ManualMatchStatsModal
  ],
  templateUrl: './matches.html',
  styleUrl: './matches.scss',
  styles: [`
    .live-line {
      display: flex;
      align-items: center;
      gap: 8px;
      margin: 10px 0 0;
      color: #8fd48f;
      font-size: 12px;
      font-weight: 700;
      letter-spacing: 0.04em;
      text-transform: uppercase;
    }
    .live-line.closed { color: #ffb4b4; }
    .live-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: #3dff8a;
      box-shadow: 0 0 10px rgba(61, 255, 138, 0.8);
    }
    .live-dot.refreshing { animation: live-pulse 0.8s ease-in-out infinite; }
    @keyframes live-pulse {
      0%, 100% { opacity: 1; transform: scale(1); }
      50% { opacity: 0.35; transform: scale(0.7); }
    }
    .match-status {
      margin-top: 4px;
      font-size: 11px;
      letter-spacing: 0.08em;
      text-transform: uppercase;
    }
    .match-card.finished { border-color: rgba(61, 255, 138, 0.28); }
    .result-card {
      margin: 0 0 16px;
      padding: 18px;
      border: 1px solid var(--neon-primary);
      border-radius: 18px;
      background: radial-gradient(circle at top, rgba(0,255,136,.12), transparent 50%), rgba(0,255,136,.04);
      text-align: center;
    }
    .result-card h3 { margin: 0 0 6px; font-size: 16px; font-weight: 900; }
    .result-card p { margin: 0 0 14px; color: var(--text-secondary); font-size: 13px; }
    .result-card button {
      width: 100%;
      margin-top: 14px;
      padding: 12px;
      border: none;
      border-radius: 12px;
      background: var(--neon-primary);
      color: var(--text-inverse);
      font-weight: 900;
      cursor: pointer;
    }
    .result-card button:disabled { opacity: .55; cursor: not-allowed; }
    .result-inputs {
      display: grid;
      grid-template-columns: 1fr auto 1fr;
      align-items: end;
      gap: 10px;
    }
    .result-inputs label {
      display: flex;
      flex-direction: column;
      gap: 6px;
      font-size: 12px;
      color: var(--text-secondary);
    }
    .result-inputs input {
      width: 100%;
      padding: 10px;
      border: 1px solid var(--border-secondary);
      border-radius: 10px;
      background: rgba(0, 0, 0, 0.28);
      color: var(--text-main);
      font-size: 22px;
      font-weight: 900;
      text-align: center;
    }
    .result-inputs span { padding-bottom: 10px; font-size: 22px; font-weight: 900; }
  `]
})
export class Matches implements OnInit, OnDestroy {

  summaryFile: File | null = null;
  homePerformanceFile: File | null = null;
  awayPerformanceFile: File | null = null;

  manualStatsMatch: MatchView | null = null;

  summaryOcrPreview: MatchSummaryStatsOcrPreview | null = null;

  homePerformanceOcrPreview:
    TeamPerformanceOcrPreview | null = null;

  awayPerformanceOcrPreview:
    TeamPerformanceOcrPreview | null = null;

  isLoading = false;
  isRefreshing = false;
  isUploadingResult = false;
  isSubmittingResult = false;
  errorMessage = '';
  liveMessage = '';
  seasonClosed = false;

  selectedSeasonId = '';
  selectedWeek = 1;

  weeks: MatchWeekDto[] = [];
  weekOptions: NeonDropdownOption[] = [];

  matches: MatchView[] = [];
  selectedMatch: MatchView | null = null;

  selectedResultFiles: File[] = [];

  statsUploadResult:
    MatchStatsUploadResult | null = null;

  resultHomeScore: number | null = null;
  resultAwayScore: number | null = null;

  currentUserId = '';
  isAdmin = false;

  private subscription = new Subscription();

  constructor(
    private readonly matchService: MatchService,
    private readonly seasonService: SeasonService,
    private readonly matchStatsService: MatchStatsService,
    private readonly userService: UserService,
    private readonly realtime: RealtimeService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.userService.getMe().pipe(take(1)).subscribe({
      next: me => {
        this.currentUserId = me?.userId ?? '';
        this.isAdmin = (me?.role ?? '').toLowerCase() === 'admin';
        this.cdr.detectChanges();
      }
    });

    this.loadCurrentSeason();

    this.subscription.add(
      this.realtime.draftEvents$.subscribe(event => {
        if ((event?.eventType ?? '').toLowerCase() !== 'matchresult') {
          return;
        }

        if (event.seasonId && this.selectedSeasonId && event.seasonId !== this.selectedSeasonId) {
          return;
        }

        this.liveMessage = 'Resultado actualizado';
        this.loadMatches(false);
      })
    );
  }

  ngOnDestroy(): void {
    if (this.selectedSeasonId) {
      void this.realtime.leaveSeason(this.selectedSeasonId);
    }

    this.subscription.unsubscribe();
  }

  loadCurrentSeason(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.matches = [];
    this.selectedMatch = null;

    this.clearUploadState();
    this.cdr.detectChanges();

    this.seasonService.getAll().subscribe({
      next: seasons => {
        const currentSeason =
          seasons.find(x => x.isCurrent) ??
          seasons[0];

        if (!currentSeason) {
          this.errorMessage = 'No season found.';
          this.isLoading = false;
          this.cdr.detectChanges();
          return;
        }

        this.selectedSeasonId =
          currentSeason.seasonId;

        this.seasonClosed =
          currentSeason.phase === 'Closed' ||
          !currentSeason.isActive;

        void this.realtime.joinSeason(this.selectedSeasonId);

        this.loadWeeks();
      },
      error: () => {
        this.errorMessage =
          'Could not load season.';

        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadWeeks(): void {
    this.matchService
      .getWeeks(this.selectedSeasonId)
      .subscribe({
        next: weeks => {
          this.weeks = weeks;

          this.weekOptions = weeks.map(week => ({
            label: week.label,
            value: week.value.toString()
          }));

          if (weeks.length === 0) {
            this.matches = [];
            this.selectedMatch = null;
            this.isLoading = false;
            this.cdr.detectChanges();
            return;
          }

          this.selectedWeek =
            Number(weeks[0].value);

          this.loadMatches(true);
        },
        error: () => {
          this.errorMessage =
            'Could not load match weeks.';

          this.isLoading = false;
          this.cdr.detectChanges();
        }
      });
  }

  loadMatches(showLoader = true): void {
    const selectedId = this.selectedMatch?.id;

    if (showLoader) {
      this.isLoading = true;
      this.matches = [];
      this.selectedMatch = null;
      this.clearUploadState();
    } else {
      this.isRefreshing = true;
    }

    this.errorMessage = '';
    this.cdr.detectChanges();

    this.matchService
      .getByWeek(
        this.selectedSeasonId,
        Number(this.selectedWeek)
      )
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.isRefreshing = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: matches => {
          this.matches = matches.map(match =>
            this.mapMatch(match)
          );

          const preserved =
            this.matches.find(x => x.id === selectedId) ??
            this.matchOfTheWeek ??
            this.matches[0] ??
            null;

          this.selectedMatch = preserved;
          this.syncResultInputs(preserved);

          this.cdr.detectChanges();
        },
        error: () => {
          this.errorMessage =
            'Could not load matches.';

          this.matches = [];
          this.selectedMatch = null;

          this.cdr.detectChanges();
        }
      });
  }

  onWeekDropdownChange(value: string): void {
    this.selectedWeek = Number(value);
    this.loadMatches(true);
  }

  selectMatch(match: MatchView): void {
    this.selectedMatch = match;
    this.syncResultInputs(match);
    this.clearUploadState();
  }

  isFinished(match?: MatchView | null): boolean {
    return (match?.status ?? '').toLowerCase() === 'finished';
  }

  canSubmitResult(match?: MatchView | null): boolean {
    if (!match || this.seasonClosed) {
      return false;
    }

    const status = (match.status ?? '').toLowerCase();
    if (status === 'cancelled') {
      return false;
    }

    if (this.isAdmin) {
      return true;
    }

    if (!this.currentUserId) {
      return false;
    }

    return (
      match.homeUserId === this.currentUserId ||
      match.awayUserId === this.currentUserId
    );
  }

  submitResult(): void {
    if (!this.selectedMatch) {
      this.errorMessage = 'Please select a match.';
      return;
    }

    if (!this.canSubmitResult(this.selectedMatch)) {
      this.errorMessage =
        this.seasonClosed
          ? 'This season is closed.'
          : 'Only match participants or an admin can submit the result.';
      return;
    }

    if (
      this.resultHomeScore === null ||
      this.resultAwayScore === null ||
      this.resultHomeScore < 0 ||
      this.resultAwayScore < 0
    ) {
      this.errorMessage = 'Enter a valid score.';
      return;
    }

    this.isSubmittingResult = true;
    this.errorMessage = '';

    this.matchService
      .uploadResult(this.selectedMatch.id, {
        homeScore: Number(this.resultHomeScore),
        awayScore: Number(this.resultAwayScore)
      })
      .pipe(
        finalize(() => {
          this.isSubmittingResult = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: () => {
          this.liveMessage = 'Resultado guardado';
          this.loadMatches(false);
        },
        error: error => {
          this.errorMessage =
            error?.error?.message ??
            'Could not save match result.';
          this.cdr.detectChanges();
        }
      });
  }

  previewUploadedResult(): void {
    if (!this.selectedMatch) {
      this.errorMessage =
        'Please select a match.';

      return;
    }

    if (!this.canSubmitResult(this.selectedMatch)) {
      this.errorMessage =
        'Only match participants or an admin can upload stats.';
      return;
    }

    if (
      !this.summaryFile ||
      !this.homePerformanceFile ||
      !this.awayPerformanceFile
    ) {
      this.errorMessage =
        'Please select Summary, Home Team Performance and Away Team Performance.';

      return;
    }

    this.isUploadingResult = true;
    this.errorMessage = '';

    this.summaryOcrPreview = null;
    this.homePerformanceOcrPreview = null;
    this.awayPerformanceOcrPreview = null;

    forkJoin({
      summary:
        this.matchStatsService
          .previewMatchSummary(
            this.summaryFile
          ),

      homePerformance:
        this.matchStatsService
          .previewTeamPerformance(
            this.homePerformanceFile
          ),

      awayPerformance:
        this.matchStatsService
          .previewTeamPerformance(
            this.awayPerformanceFile
          )
    })
      .pipe(
        finalize(() => {
          this.isUploadingResult = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: result => {
          this.summaryOcrPreview =
            result.summary;

          this.homePerformanceOcrPreview =
            result.homePerformance;

          this.awayPerformanceOcrPreview =
            result.awayPerformance;

          this.manualStatsMatch =
            this.selectedMatch;

          this.cdr.detectChanges();
        },
        error: error => {
          console.error(error);

          this.errorMessage =
            error?.error?.message ??
            'Could not read match screenshots.';

          this.summaryOcrPreview = null;
          this.homePerformanceOcrPreview = null;
          this.awayPerformanceOcrPreview = null;
          this.manualStatsMatch = null;

          this.cdr.detectChanges();
        }
      });
  }

  confirmUploadedResult(): void {
    this.previewUploadedResult();
  }

  onSummarySelected(event: Event): void {
    const input =
      event.target as HTMLInputElement;

    this.summaryFile =
      input.files?.[0] ?? null;

    this.summaryOcrPreview = null;
  }

  onHomePerformanceSelected(
    event: Event
  ): void {
    const input =
      event.target as HTMLInputElement;

    this.homePerformanceFile =
      input.files?.[0] ?? null;

    this.homePerformanceOcrPreview = null;
  }

  onAwayPerformanceSelected(
    event: Event
  ): void {
    const input =
      event.target as HTMLInputElement;

    this.awayPerformanceFile =
      input.files?.[0] ?? null;

    this.awayPerformanceOcrPreview = null;
  }

  openManualStats(): void {
    if (!this.selectedMatch) {
      this.errorMessage =
        'Please select a match.';

      return;
    }

    if (!this.canSubmitResult(this.selectedMatch)) {
      this.errorMessage =
        'Only match participants or an admin can enter stats.';
      return;
    }

    this.errorMessage = '';

    this.summaryOcrPreview = null;
    this.homePerformanceOcrPreview = null;
    this.awayPerformanceOcrPreview = null;

    this.manualStatsMatch =
      this.selectedMatch;

    this.cdr.detectChanges();
  }

  closeManualStats(): void {
    this.manualStatsMatch = null;
    this.cdr.detectChanges();
  }

  onManualStatsSaved(): void {
    this.manualStatsMatch = null;
    this.liveMessage = 'Estadísticas guardadas';
    this.loadMatches(false);
  }

  get selectedWeekValue(): string {
    return this.selectedWeek.toString();
  }

  get filteredMatches(): MatchView[] {
    return this.matches;
  }

  get matchOfTheWeek(): MatchView | null {
    return (
      this.matches.find(
        x => x.isMatchOfTheWeek
      ) ??
      this.matches[0] ??
      null
    );
  }

  get regularMatches(): MatchView[] {
    const matchOfTheWeek =
      this.matchOfTheWeek;

    if (!matchOfTheWeek) {
      return this.matches;
    }

    return this.matches.filter(
      x => x.id !== matchOfTheWeek.id
    );
  }

  private syncResultInputs(match: MatchView | null): void {
    this.resultHomeScore = match?.homeScore ?? null;
    this.resultAwayScore = match?.awayScore ?? null;
  }

  private clearUploadState(): void {
    this.summaryFile = null;
    this.homePerformanceFile = null;
    this.awayPerformanceFile = null;

    this.selectedResultFiles = [];

    this.statsUploadResult = null;

    this.summaryOcrPreview = null;
    this.homePerformanceOcrPreview = null;
    this.awayPerformanceOcrPreview = null;
  }

  private mapMatch(
    match: MatchDto
  ): MatchView {
    return {
      id: match.matchId,
      weekNumber: match.weekNumber,
      isMatchOfTheWeek:
        match.isMatchOfTheWeek,
      status: match.status,

      arena: match.arena,
      mode: match.mode,
      date: match.matchDate,
      time: match.matchTime,

      homeScore: match.homeScore,
      awayScore: match.awayScore,
      homeUserId: match.home?.userId,
      awayUserId: match.away?.userId,

      home: {
        name: match.home.name,
        club: match.home.club,
        logo: match.home.logoUrl,
        stats: match.home.stats
      },

      away: {
        name: match.away.name,
        club: match.away.club,
        logo: match.away.logoUrl,
        stats: match.away.stats
      }
    };
  }
}

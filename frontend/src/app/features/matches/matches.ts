import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Observable, Subscription, finalize, forkJoin, take } from 'rxjs';

import { MatchService } from '../../core/services/match.service';
import { SeasonService } from '../../core/services/season.service';
import { MatchStatsService } from '../../core/services/match-stats.service';
import { UserService } from '../../core/services/user.service';
import { RealtimeService } from '../../core/services/realtime.service';

import { AppLoading } from '../../shared/components/app-loading/app-loading';

import {
  MatchDto,
  MatchWeekDto,
  PendingMatchResult
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
import { ConfirmDialogService } from '../../shared/components/confirm-dialog/confirm-dialog.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LocalizedDatePipe } from '../../core/i18n/localized-date.pipe';
import { EmptyState } from '../../shared/components/empty-state/empty-state';

@Component({
  selector: 'app-matches',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    NeonDropdownComponent,
    AppLoading,
    ManualMatchStatsModal,
    TranslatePipe, LocalizedDatePipe, EmptyState
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
  errorMessage = '';
  liveMessage = '';
  seasonClosed = false;

  selectedSeasonId = '';
  selectedWeek = 1;

  weeks: MatchWeekDto[] = [];
  weekOptions: NeonDropdownOption[] = [];

  matches: MatchView[] = [];

  /** Client-side filter applied to the selected week's matches. */
  statusFilter: 'all' | 'upcoming' | 'finished' = 'all';

  readonly statusFilterOptions: { value: 'all' | 'upcoming' | 'finished'; label: string }[] = [
    { value: 'all', label: 'All' },
    { value: 'upcoming', label: 'Upcoming' },
    { value: 'finished', label: 'Finished' }
  ];
  selectedMatch: MatchView | null = null;

  selectedResultFiles: File[] = [];

  statsUploadResult:
    MatchStatsUploadResult | null = null;

  currentUserId = '';

  pendingProposal: PendingMatchResult | null = null;
  isLoadingPending = false;
  isSubmittingProposal = false;
  isReviewingProposal = false;
  proposalMessage = '';
  isAdmin = false;

  private subscription = new Subscription();

  constructor(
    private readonly matchService: MatchService,
    private readonly seasonService: SeasonService,
    private readonly matchStatsService: MatchStatsService,
    private readonly userService: UserService,
    private readonly realtime: RealtimeService,
    private readonly confirmDialog: ConfirmDialogService,
    private readonly route: ActivatedRoute,
    private readonly cdr: ChangeDetectorRef
  ) {}

  /** Deep link from global search: ?seasonId=&week=&matchId= */
  private requestedSeasonId: string | null = null;
  private requestedWeek: number | null = null;
  private requestedMatchId: string | null = null;

  ngOnInit(): void {
    this.userService.getMe().pipe(take(1)).subscribe({
      next: me => {
        this.currentUserId = me?.userId ?? '';
        this.isAdmin = (me?.role ?? '').toLowerCase() === 'admin';
        this.cdr.detectChanges();
      }
    });

    this.subscription.add(
      this.route.queryParamMap.subscribe(params => {
        const week = Number(params.get('week'));

        this.requestedSeasonId = params.get('seasonId')?.trim() || null;
        this.requestedWeek = Number.isFinite(week) && week > 0 ? week : null;
        this.requestedMatchId = params.get('matchId')?.trim() || null;

        if (this.requestedMatchId) {
          // Make sure the linked match is not hidden by the status filter.
          this.statusFilter = 'all';
        }

        this.loadCurrentSeason();
      })
    );

    this.subscription.add(
      this.realtime.draftEvents$.subscribe(event => {
        if ((event?.eventType ?? '').toLowerCase() !== 'matchresult') {
          return;
        }

        if (event.seasonId && this.selectedSeasonId && event.seasonId !== this.selectedSeasonId) {
          return;
        }

        this.liveMessage = 'Result updated';
        this.loadMatches(false);
        this.loadPendingProposal();
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
          (this.requestedSeasonId
            ? seasons.find(x => x.seasonId === this.requestedSeasonId)
            : undefined) ??
          seasons.find(x => x.isCurrent) ??
          seasons[0];

        if (!currentSeason) {
          this.errorMessage = 'No season found.';
          this.isLoading = false;
          this.cdr.detectChanges();
          return;
        }

        if (this.selectedSeasonId && this.selectedSeasonId !== currentSeason.seasonId) {
          void this.realtime.leaveSeason(this.selectedSeasonId);
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

          const requestedWeek = weeks.find(
            week => Number(week.value) === this.requestedWeek
          );

          this.selectedWeek =
            Number((requestedWeek ?? weeks[0]).value);

          this.requestedWeek = null;

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

          const requestedMatch = this.requestedMatchId
            ? this.matches.find(x => x.id === this.requestedMatchId)
            : undefined;

          this.requestedMatchId = null;

          const preserved =
            requestedMatch ??
            this.matches.find(x => x.id === selectedId) ??
            this.matchOfTheWeek ??
            this.matches[0] ??
            null;

          const changedMatch = this.selectedMatch?.id !== preserved?.id;

          this.selectedMatch = preserved;

          if (changedMatch || !this.pendingProposal) {
            this.loadPendingProposal();
          }

          this.cdr.detectChanges();

          if (requestedMatch) {
            setTimeout(() =>
              document
                .querySelector('.match-card.selected')
                ?.scrollIntoView({ behavior: 'smooth', block: 'center' })
            );
          }
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
    this.clearUploadState();
    this.loadPendingProposal();
  }

  /** Loads the pending OCR proposal (if any) for the selected match. */
  loadPendingProposal(): void {
    const matchId = this.selectedMatch?.id;

    this.proposalMessage = '';

    if (!matchId) {
      this.pendingProposal = null;
      return;
    }

    this.isLoadingPending = true;

    this.matchService.getPendingResultProposal(matchId)
      .pipe(
        take(1),
        finalize(() => {
          this.isLoadingPending = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: proposal => {
          if (this.selectedMatch?.id !== matchId) {
            return;
          }

          this.pendingProposal = proposal ?? null;
        },
        error: () => {
          if (this.selectedMatch?.id === matchId) {
            this.pendingProposal = null;
          }
        }
      });
  }

  /** Uploads the 3 screenshots as a proposal that the opponent must accept. */
  submitForApproval(): void {
    const match = this.selectedMatch;

    if (!match || !this.summaryFile || !this.homePerformanceFile || !this.awayPerformanceFile) {
      this.errorMessage = 'Select all three screenshots first.';
      return;
    }

    if (this.pendingProposal || this.isSubmittingProposal) {
      return;
    }

    this.isSubmittingProposal = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.matchService.submitResultProposal(
      match.id,
      this.summaryFile,
      this.homePerformanceFile,
      this.awayPerformanceFile
    )
      .pipe(
        take(1),
        finalize(() => {
          this.isSubmittingProposal = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: proposal => {
          this.pendingProposal = proposal;
          this.clearUploadState();
          this.proposalMessage = 'Result submitted. Waiting for opponent to accept.';
        },
        error: err => {
          this.errorMessage = err?.error?.message || 'Could not submit the result.';
          this.loadPendingProposal();
        }
      });
  }

  acceptProposal(): void {
    this.reviewProposal(true);
  }

  async rejectProposal(): Promise<void> {
    if (!this.pendingProposal?.canReview || this.isReviewingProposal) {
      return;
    }

    const ok = await this.confirmDialog.confirm({
      title: 'Reject this result?',
      description: 'The proposed score and stats will be discarded and the uploader will need to submit again.',
      confirmLabel: 'Reject result'
    });

    if (ok) {
      this.reviewProposal(false);
    }
  }

  private reviewProposal(accept: boolean): void {
    const match = this.selectedMatch;
    const proposal = this.pendingProposal;

    if (!match || !proposal || !proposal.canReview || this.isReviewingProposal) {
      return;
    }

    this.isReviewingProposal = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    const request: Observable<unknown> = accept
      ? this.matchService.acceptResultProposal(match.id, proposal.pendingMatchResultId)
      : this.matchService.rejectResultProposal(match.id, proposal.pendingMatchResultId);

    request
      .pipe(
        take(1),
        finalize(() => {
          this.isReviewingProposal = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: () => {
          this.pendingProposal = null;
          this.proposalMessage = accept ? 'Result accepted.' : 'Result rejected.';
          this.loadMatches(false);
        },
        error: err => {
          this.errorMessage = err?.error?.message || 'Could not review the result.';
          this.loadPendingProposal();
        }
      });
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
    this.liveMessage = 'Stats saved';
    this.loadMatches(false);
  }

  get selectedWeekValue(): string {
    return this.selectedWeek.toString();
  }

  setStatusFilter(filter: 'all' | 'upcoming' | 'finished'): void {
    this.statusFilter = filter;
    this.cdr.detectChanges();
  }

  get filteredMatches(): MatchView[] {
    if (this.statusFilter === 'finished') {
      return this.matches.filter(x => this.isFinished(x));
    }

    if (this.statusFilter === 'upcoming') {
      return this.matches.filter(x => {
        const status = (x.status ?? '').toLowerCase();
        return status !== 'finished' && status !== 'cancelled';
      });
    }

    return this.matches;
  }

  get matchOfTheWeek(): MatchView | null {
    const list = this.filteredMatches;

    return (
      list.find(
        x => x.isMatchOfTheWeek
      ) ??
      list[0] ??
      null
    );
  }

  /** First letter of a team name, used when the logo is missing. */
  teamInitial(name?: string | null): string {
    return (name ?? '').trim().charAt(0).toUpperCase() || '?';
  }

  /** Falls back to the placeholder when a logo URL fails to load. */
  onLogoError(team: { logo?: string | null }): void {
    team.logo = '';
    this.cdr.detectChanges();
  }

  get regularMatches(): MatchView[] {
    const matchOfTheWeek =
      this.matchOfTheWeek;

    if (!matchOfTheWeek) {
      return this.filteredMatches;
    }

    return this.filteredMatches.filter(
      x => x.id !== matchOfTheWeek.id
    );
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

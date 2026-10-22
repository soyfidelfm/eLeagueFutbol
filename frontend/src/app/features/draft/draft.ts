import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize, forkJoin, Observable, Subscription, take } from 'rxjs';

import { DraftService } from '../../core/services/draft.service';
import { SeasonService } from '../../core/services/season.service';
import { RealtimeService } from '../../core/services/realtime.service';
import { AppLoading } from '../../shared/components/app-loading/app-loading';
import {
  DraftCenter,
  DraftOrder,
  DraftPick,
  DraftPlayer,
  DraftPlayerPoolFilter,
  DraftSquadPlayer
} from '../../core/models/draft.model';
import { TranslationService } from '../../core/i18n/translation.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { UserService } from '../../core/services/user.service';
import { ConfirmDialogService } from '../../shared/components/confirm-dialog/confirm-dialog.service';
import { DraftState } from '../../core/models/draft.model';

/** Countdown colour band: >50% normal, <=50% warning (yellow), <=25% danger (red). */
export type DraftTimerLevel = 'normal' | 'warning' | 'danger';


interface SquadSlot {
  position: string;
  player?: DraftSquadPlayer;
}

@Component({
  selector: 'app-draft',
  standalone: true,
  imports: [CommonModule, FormsModule, AppLoading,
    TranslatePipe, EmptyState
  ],
  templateUrl: './draft.html',
  styleUrls: ['./draft.scss', './draft-controls.scss']
})
export class Draft implements OnInit, OnDestroy {
  isLoading = true;
  isPicking = false;
  isStartingDraft = false;
  isLotteryRunning = false;
  isPoolDropdownOpen = false;

  allDraftPicks: DraftPick[] = [];
  isDraftPicksModalOpen = false;
  isLoadingDraftPicks = false;


  errorMessage = '';

  searchTerm = '';
  selectedPosition = 'All';
  currentPage = 1;
  pageSize = 9;

  secondsRemaining = 0;
  timerText = '00:00';
  timerLevel: DraftTimerLevel = 'normal';

  /** Admin draft controls. */
  isAdmin = false;
  isAdminBusy = false;
  adminTimerEnabled = true;
  adminPickSeconds = 300;
  readonly minPickSeconds = 10;
  readonly maxPickSeconds = 600;

  /** Players whose photo failed to load; they render the initials placeholder instead. */
  private failedImageIds = new Set<string>();
  private countdownDeadlineMs = 0;
  private countdownExpiredReloadDone = false;

  lotteryDrawNumber = 0;

  draftCenter: DraftCenter | null = null;

  draftOrder: DraftOrder[] = [];
  players: DraftPlayer[] = [];
  lastPicks: DraftPick[] = [];

  poolFilter: DraftPlayerPoolFilter = 'ClubOnly';

  seasonId = '';

  private timerId?: number;
  private lotteryTimeoutId?: number;
  private subscription = new Subscription();
  private joinedSeasonId = '';

  positions = ['All', 'ST', 'LW', 'RW', 'CM', 'CDM', 'CB', 'LB', 'RB', 'GK'];

  squadSlots: SquadSlot[] = [
    { position: 'ST' },
    { position: 'LW' },
    { position: 'RW' },
    { position: 'CM' },
    { position: 'CM' },
    { position: 'CDM' },
    { position: 'LB' },
    { position: 'CB' },
    { position: 'CB' },
    { position: 'RB' },
    { position: 'GK' }
  ];

  constructor(
    private route: ActivatedRoute,
    private draftService: DraftService,
    private seasonService: SeasonService,
    private realtime: RealtimeService,
    private cdr: ChangeDetectorRef,
    private i18n: TranslationService,
    private userService: UserService,
    private confirmDialog: ConfirmDialogService
  ) { }

  ngOnInit(): void {
    this.userService.getMe().pipe(take(1)).subscribe({
      next: me => {
        this.isAdmin = (me?.role ?? '').toLowerCase() === 'admin';
        this.cdr.detectChanges();
      }
    });

    this.subscription.add(
      this.realtime.draftEvents$.subscribe(event => {
        if (!event || (this.seasonId && event.seasonId && event.seasonId !== this.seasonId)) {
          return;
        }

        this.handleDraftEvent(event.eventType);
      })
    );

    this.route.queryParamMap
      .pipe(take(1))
      .subscribe(params => {
        this.seasonId = params.get('seasonId') ?? '';

        if (this.seasonId) {
          this.loadDraft();
          void this.joinSeasonRoom();
          return;
        }

        this.loadCurrentSeasonAndDraft();
      });
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();

    if (this.timerId) {
      window.clearInterval(this.timerId);
    }

    if (this.lotteryTimeoutId) {
      window.clearTimeout(this.lotteryTimeoutId);
    }

    if (this.joinedSeasonId) {
      void this.realtime.leaveSeason(this.joinedSeasonId);
    }
  }

  togglePoolDropdown(): void {
    this.isPoolDropdownOpen = !this.isPoolDropdownOpen;
  }

  getPoolFilterLabel(): string {
    switch (this.poolFilter) {
      case 'IncludeFreeAgents':
        return 'Club + Free Agents';

      case 'FreeAgentsOnly':
        return 'Free Agents Only';

      case 'ClubOnly':
      default:
        return 'Club Players';
    }
  }

  setPoolFilter(poolFilter: DraftPlayerPoolFilter): void {
    this.poolFilter = poolFilter;
    this.isPoolDropdownOpen = false;
    this.currentPage = 1;
    this.loadPlayers();
  }

  get draftedCount(): number {
    return this.draftCenter?.mySquad.totalPlayers ?? 0;
  }

  get filteredPlayers(): DraftPlayer[] {
    return this.players;
  }

  get pagedPlayers(): DraftPlayer[] {
    return this.players;
  }

  get totalPages(): number {
    return this.players.length < this.pageSize
      ? this.currentPage
      : this.currentPage + 1;
  }

  get currentRound(): number {
    return this.draftCenter?.state.currentRound ?? 0;
  }

  get playersPerTeam(): number {
    return this.draftCenter?.state.playersPerTeam ?? this.squadSlots.length;
  }

  get totalTeams(): number {
    return this.draftCenter?.state.totalTeams ?? 0;
  }

  get currentPickNumber(): number {
    return this.draftCenter?.state.currentPickNumber ?? 0;
  }

  get currentClubName(): string {
    return this.draftCenter?.state.currentClubName || 'Waiting...';
  }

  get isDraftActive(): boolean {
    return this.draftCenter?.state.isActive ?? false;
  }

  get isDraftPaused(): boolean {
    return this.draftCenter?.state.isPaused ?? false;
  }

  get isDraftCompleted(): boolean {
    return this.draftCenter?.state.isCompleted ?? false;
  }

  get isTimerEnabled(): boolean {
    return this.draftCenter?.state.timerEnabled ?? true;
  }

  get pickSeconds(): number {
    return this.draftCenter?.state.pickSeconds ?? 0;
  }

  get hasDraftOrder(): boolean {
    return (this.draftCenter?.draftOrder?.length ?? 0) > 0;
  }

  get isAdminPickSecondsValid(): boolean {
    const value = Number(this.adminPickSeconds);
    return Number.isInteger(value) && value >= this.minPickSeconds && value <= this.maxPickSeconds;
  }

  get currentBudget(): number {
    return this.draftCenter?.currentBudget ?? 0;
  }

  get isMyTurn(): boolean {
    if (!this.draftCenter) {
      return false;
    }

    return this.draftCenter.myUserId === this.draftCenter.state.currentUserId;
  }

  get lotteryTitle(): string {
    if (!this.isLotteryRunning) {
      return '';
    }

    return this.i18n.t('Draft Lottery Draw {n} of 3', { n: this.lotteryDrawNumber });
  }

  getPlayerImageUrl(player: DraftPlayer): string {
    return player.imageUrl?.trim() ?? '';
  }

  hasPlayerPhoto(player: DraftPlayer): boolean {
    return !!this.getPlayerImageUrl(player) && !this.failedImageIds.has(player.gamePlayerId);
  }

  onPlayerImageError(player: DraftPlayer): void {
    this.failedImageIds.add(player.gamePlayerId);
    this.cdr.detectChanges();
  }

  getPlayerInitials(name: string): string {
    const parts = (name ?? '').trim().split(/\s+/).filter(Boolean);

    if (parts.length === 0) {
      return '?';
    }

    const first = parts[0][0] ?? '';
    const last = parts.length > 1 ? parts[parts.length - 1][0] ?? '' : '';

    return (first + last).toUpperCase();
  }

  loadCurrentSeasonAndDraft(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.seasonService.getAll()
      .pipe(
        take(1),
        finalize(() => {
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: seasons => {
          const currentSeason = (seasons ?? []).find(
            x => x.isCurrent && x.isActive
          );

          if (!currentSeason?.seasonId) {
            this.isLoading = false;
            this.errorMessage = 'No active season found.';
            this.cdr.detectChanges();
            return;
          }

          this.seasonId = currentSeason.seasonId;
          this.loadDraft();
          void this.joinSeasonRoom();
        },
        error: err => {
          console.error('Error loading current season:', err);
          this.isLoading = false;
          this.errorMessage = 'Could not load current season.';
          this.cdr.detectChanges();
        }
      });
  }

  loadDraft(): void {
    if (!this.seasonId) {
      this.loadCurrentSeasonAndDraft();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    forkJoin({
      center: this.draftService.getCenter(this.seasonId),
      players: this.draftService.getPlayers(
        this.seasonId,
        this.searchTerm,
        this.selectedPosition === 'All' ? '' : this.selectedPosition,
        this.currentPage,
        this.pageSize,
        this.poolFilter
      )
    })
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: ({ center, players }) => {
          this.draftCenter = center;
          this.draftOrder = center.draftOrder ?? [];
          this.lastPicks = center.recentPicks ?? [];
          this.players = players ?? [];
          this.syncAdminInputs(center.state);

          this.mapMySquadToSlots();
          this.startTimer(center.state);

          this.cdr.detectChanges();
        },
        error: err => {
          console.error('Error loading draft:', err);
          this.errorMessage = err?.error?.message || 'Error loading draft.';
          this.cdr.detectChanges();
        }
      });
  }

  startDraft(): void {
    if (!this.seasonId || this.isLotteryRunning || this.isStartingDraft) {
      return;
    }

    this.errorMessage = '';

    // Si ya existe el orden del draft, ahora sí iniciar el draft.
    if (this.draftOrder.length > 0) {
      this.isStartingDraft = true;

      this.draftService.startDraft(this.seasonId)
        .pipe(
          finalize(() => {
            this.isStartingDraft = false;
            this.cdr.detectChanges();
          })
        )
        .subscribe({
          next: center => {
            this.draftCenter = center;
            this.draftOrder = center.draftOrder ?? [];
            this.lastPicks = center.recentPicks ?? [];

            this.mapMySquadToSlots();
            this.startTimer(center.state);
            this.loadPlayers();

            this.showToast('🚀 ' + this.i18n.t('Draft started.'));
            this.cdr.detectChanges();
          },
          error: err => {
            console.error('Error starting draft:', err);

            this.errorMessage =
              err?.error?.message ||
              'Could not start draft.';

            this.cdr.detectChanges();
          }
        });

      return;
    }

    // Si aún no existe el orden, correr la lotería.
    this.isLotteryRunning = true;
    this.isStartingDraft = true;
    this.lotteryDrawNumber = 1;

    this.showToast('🎲 ' + this.i18n.t('Starting draft lottery...'));
    this.runLotteryDraw(1);
  }

  private runLotteryDraw(drawNumber: number): void {
    this.lotteryDrawNumber = drawNumber;
    this.cdr.detectChanges();

    this.draftService.previewDraftOrder(
      this.seasonId,
      drawNumber
    )
      .subscribe({
        next: preview => {
          this.draftOrder = preview.draftOrder ?? [];
          this.lastPicks = [];

          this.cdr.detectChanges();

          if (drawNumber < 3) {
            this.showToast('🎲 ' + this.i18n.t('Draft lottery draw {n} of 3...', { n: drawNumber }));

            this.lotteryTimeoutId = window.setTimeout(() => {
              this.runLotteryDraw(drawNumber + 1);
            }, 5000);

            return;
          }

          const finalUserIds = this.draftOrder.map(x => x.userId);

          this.draftService.generateDraftOrder(
            this.seasonId,
            finalUserIds
          )
            .pipe(
              finalize(() => {
                this.isLotteryRunning = false;
                this.isStartingDraft = false;
                this.cdr.detectChanges();
              })
            )
            .subscribe({
              next: center => {
                this.draftCenter = center;
                this.draftOrder = center.draftOrder ?? [];
                this.lastPicks = center.recentPicks ?? [];

                this.mapMySquadToSlots();
                this.loadPlayers();

                this.showToast('🏆 ' + this.i18n.t('Final draft order saved. Ready to start.'));
                this.cdr.detectChanges();
              },
              error: err => {
                console.error('Error saving final draft order:', err);

                this.errorMessage =
                  err?.error?.message ||
                  'Could not save final draft order.';

                this.cdr.detectChanges();
              }
            });
        },
        error: err => {
          console.error('Error generating draft order preview:', err);

          this.errorMessage =
            err?.error?.message ||
            'Could not generate draft order preview.';

          this.isLotteryRunning = false;
          this.isStartingDraft = false;

          this.cdr.detectChanges();
        }
      });
  }

  loadPlayers(): void {
    if (!this.seasonId) {
      return;
    }

    this.draftService.getPlayers(
      this.seasonId,
      this.searchTerm,
      this.selectedPosition === 'All' ? '' : this.selectedPosition,
      this.currentPage,
      this.pageSize,
      this.poolFilter
    )
      .pipe(
        finalize(() => {
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: players => {
          this.players = players ?? [];
        },
        error: err => {
          console.error('Error loading players:', err);
          this.errorMessage = err?.error?.message || 'Error loading players.';
        }
      });
  }

  setPosition(position: string): void {
    this.selectedPosition = position;
    this.currentPage = 1;
    this.loadPlayers();
  }

  nextPage(): void {
    this.currentPage++;
    this.loadPlayers();
  }

  previousPage(): void {
    if (this.currentPage <= 1) {
      return;
    }

    this.currentPage--;
    this.loadPlayers();
  }

  draftPlayer(player: DraftPlayer): void {
    if (!this.isMyTurn) {
      this.showToast(
        '⏳ ' + this.i18n.t('You\'re on deck. {club} is currently on the clock.', { club: this.currentClubName })
      );
      return;
    }

    if (!this.seasonId) {
      this.showToast('Season was not found.');
      return;
    }

    if (!this.isDraftActive) {
      this.showToast('The draft is not active right now.');
      return;
    }

    if (this.isDraftPaused) {
      this.showToast('The draft is paused.');
      return;
    }

    if (this.isPicking) {
      return;
    }

    const playerValue = player.value ?? 0;

    if (playerValue > this.currentBudget) {
      this.showToast(
        '❌ ' + this.i18n.t('Not enough budget. {player} costs €{cost} and your budget is €{budget}.', { player: player.name, cost: playerValue.toLocaleString(), budget: this.currentBudget.toLocaleString() })
      );
      return;
    }

    this.isPicking = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.draftService.makePick(this.seasonId, player.gamePlayerId)
      .pipe(
        finalize(() => {
          this.isPicking = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: () => {
          this.loadDraft();

          if (this.isDraftPicksModalOpen) {
            this.loadDraftPicks();
          }
        },
        error: err => {
          console.error('Error making draft pick:', err);

          this.errorMessage =
            err?.error?.message ||
            this.i18n.t('Could not draft {player}. Please try again.', { player: player.name });

          this.cdr.detectChanges();
        }
      });
  }

  getPrimaryPosition(position: string): string {
    if (!position) {
      return '';
    }

    return position.split(',')[0].trim();
  }

  private handleDraftEvent(eventType: string): void {
    const type = (eventType ?? '').toLowerCase();

    if (type === 'pickmade' || type === 'pick') {
      this.showToast('New draft pick');
    } else if (type === 'started' || type === 'start') {
      this.showToast('The draft has started');
    } else if (type === 'turn' || type === 'turnchanged' || type === 'advanced') {
      this.showToast('Draft turn changed');
    } else if (type === 'skipped' || type === 'skip') {
      this.showToast('Turn skipped (time expired)');
    } else if (type === 'completed' || type === 'complete') {
      this.showToast('Draft completed');
    } else if (type === 'order' || type === 'lottery') {
      this.showToast('Draft order updated');
    } else if (type === 'paused') {
      this.showToast('The draft is paused');
    } else if (type === 'resumed') {
      this.showToast('The draft has resumed');
    } else if (type === 'stopped') {
      this.showToast('The draft was stopped');
    } else if (type === 'timerupdated') {
      this.showToast('Pick timer updated');
    }

    this.loadDraft();

    if (this.isDraftPicksModalOpen) {
      this.loadDraftPicks();
    }
  }

  private async joinSeasonRoom(): Promise<void> {
    if (!this.seasonId) {
      return;
    }

    try {
      await this.realtime.connect();
      await this.realtime.joinSeason(this.seasonId);
      this.joinedSeasonId = this.seasonId;
    } catch (err) {
      console.error('Could not join draft season hub', err);
    }
  }

  private showToast(message: string): void {
    this.errorMessage = message;
    this.cdr.detectChanges();

    setTimeout(() => {
      if (this.errorMessage === message) {
        this.errorMessage = '';
        this.cdr.detectChanges();
      }
    }, 3500);
  }

  private mapMySquadToSlots(): void {
    this.squadSlots = this.squadSlots.map(slot => ({
      position: slot.position
    }));

    const squadPlayers = this.draftCenter?.mySquad.players ?? [];

    squadPlayers.forEach(player => {
      const primaryPosition = this.getPrimaryPosition(player.position);

      const slot =
        this.squadSlots.find(x => !x.player && x.position === primaryPosition) ??
        this.squadSlots.find(x => !x.player);

      if (slot) {
        slot.player = player;
      }
    });
  }

  /**
   * Countdown driven by the server's pick deadline. The client only renders it; the server
   * (DraftTimerBackgroundService) skips the turn at 0 and broadcasts a DraftEvent.
   */
  private startTimer(state: DraftState): void {
    if (this.timerId) {
      window.clearInterval(this.timerId);
      this.timerId = undefined;
    }

    this.countdownExpiredReloadDone = false;

    if (!state.isActive) {
      this.setTimerDisplay(0, state);
      return;
    }

    if (state.isPaused || state.timerEnabled === false || !state.currentPickDeadlineUTC) {
      // Paused: the server froze the remaining seconds. Timer off: no limit.
      this.setTimerDisplay(state.secondsRemaining ?? 0, state);
      return;
    }

    const serverNow = state.serverTimeUTC ? Date.parse(state.serverTimeUTC) : NaN;
    const clockOffsetMs = Number.isFinite(serverNow) ? serverNow - Date.now() : 0;
    this.countdownDeadlineMs = Date.parse(state.currentPickDeadlineUTC);

    const tick = () => {
      const remainingMs = this.countdownDeadlineMs - (Date.now() + clockOffsetMs);
      const remaining = Math.max(0, Math.ceil(remainingMs / 1000));

      this.setTimerDisplay(remaining, state);

      if (remaining <= 0) {
        window.clearInterval(this.timerId);
        this.timerId = undefined;

        // Fallback in case the DraftEvent from the server sweep is missed.
        if (!this.countdownExpiredReloadDone) {
          this.countdownExpiredReloadDone = true;
          window.setTimeout(() => this.loadDraft(), 4000);
        }
      }
    };

    tick();
    this.timerId = window.setInterval(tick, 250);
  }

  private setTimerDisplay(remaining: number, state: DraftState): void {
    this.secondsRemaining = remaining;

    if (state.isActive && state.timerEnabled === false) {
      this.timerText = '∞';
      this.timerLevel = 'normal';
      this.cdr.detectChanges();
      return;
    }

    this.timerText = this.formatTime(remaining);

    const total = state.pickSeconds ?? 0;
    const ratio = total > 0 ? remaining / total : 1;

    this.timerLevel = !state.isActive
      ? 'normal'
      : ratio <= 0.25
        ? 'danger'
        : ratio <= 0.5
          ? 'warning'
          : 'normal';

    this.cdr.detectChanges();
  }

  private formatTime(totalSeconds: number): string {
    const safeSeconds = Math.max(0, totalSeconds);
    const minutes = Math.floor(safeSeconds / 60);
    const seconds = safeSeconds % 60;

    return `${minutes.toString().padStart(2, '0')}:${seconds
      .toString()
      .padStart(2, '0')}`;
  }

  // ===== Admin draft controls =====

  private syncAdminInputs(state: DraftState): void {
    if (this.isAdminBusy) {
      return;
    }

    this.adminTimerEnabled = state.timerEnabled ?? true;
    this.adminPickSeconds = state.pickSeconds || 300;
  }

  private applyCenter(center: DraftCenter): void {
    this.draftCenter = center;
    this.draftOrder = center.draftOrder ?? [];
    this.lastPicks = center.recentPicks ?? [];
    this.mapMySquadToSlots();
    this.startTimer(center.state);
    this.isAdminBusy = false;
    this.syncAdminInputs(center.state);
    this.cdr.detectChanges();
  }

  private runAdminAction(
    request: () => Observable<DraftCenter>,
    successMessage: string
  ): void {
    if (!this.isAdmin || !this.seasonId || this.isAdminBusy) {
      return;
    }

    this.isAdminBusy = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    request().subscribe({
      next: center => {
        this.applyCenter(center);
        this.showToast(successMessage);
      },
      error: err => {
        this.isAdminBusy = false;
        this.errorMessage = err?.error?.message || 'Could not update the draft.';
        this.cdr.detectChanges();
      }
    });
  }

  pauseDraft(): void {
    this.runAdminAction(() => this.draftService.pauseDraft(this.seasonId), 'The draft is paused');
  }

  resumeDraft(): void {
    this.runAdminAction(() => this.draftService.resumeDraft(this.seasonId), 'The draft has resumed');
  }

  async stopDraft(): Promise<void> {
    if (!this.isAdmin) {
      return;
    }

    const ok = await this.confirmDialog.confirm({
      title: 'Stop the draft?',
      description: 'The draft ends now and cannot be restarted. Picks already made are kept.',
      confirmLabel: 'Stop draft',
      danger: true
    });

    if (!ok) {
      return;
    }

    this.runAdminAction(() => this.draftService.stopDraft(this.seasonId), 'The draft was stopped');
  }

  saveTimerSettings(): void {
    if (!this.isAdminPickSecondsValid) {
      this.errorMessage = 'Seconds per pick must be between 10 and 600.';
      return;
    }

    this.runAdminAction(
      () => this.draftService.updateTimer(this.seasonId, {
        timerEnabled: this.adminTimerEnabled,
        pickSeconds: Number(this.adminPickSeconds)
      }),
      'Pick timer updated'
    );
  }

  openDraftPicksModal(): void {
    if (!this.seasonId) {
      return;
    }

    this.isDraftPicksModalOpen = true;
    this.loadDraftPicks();
  }

  closeDraftPicksModal(): void {
    this.isDraftPicksModalOpen = false;
  }

  loadDraftPicks(): void {
    if (!this.seasonId) {
      return;
    }

    this.isLoadingDraftPicks = true;

    this.draftService.getPicks(this.seasonId)
      .pipe(
        finalize(() => {
          this.isLoadingDraftPicks = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: picks => {
          this.allDraftPicks = picks ?? [];
        },
        error: err => {
          console.error('Error loading draft picks:', err);
          this.showToast('Could not load draft picks.');
        }
      });
  }


}

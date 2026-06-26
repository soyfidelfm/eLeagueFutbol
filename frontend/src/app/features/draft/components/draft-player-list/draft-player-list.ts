import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  EventEmitter,
  Input,
  OnChanges,
  OnDestroy,
  OnInit,
  Output,
  SimpleChanges
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  debounceTime,
  distinctUntilChanged,
  finalize,
  Subject,
  takeUntil
} from 'rxjs';

import { DraftService } from '../../../../core/services/draft.service';
import { DraftPlayer } from '../../../../core/models/draft.model';
import { DraftPlayerCardComponent } from './draft-player-card/draft-player-card';
import { DraftPlayerFiltersComponent } from './draft-player-filters/draft-player-filters';

@Component({
  selector: 'app-draft-player-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DraftPlayerCardComponent,
    DraftPlayerFiltersComponent
  ],
  templateUrl: './draft-player-list.html',
  styleUrl: './draft-player-list.scss'
})
export class DraftPlayerList implements OnChanges, OnInit, OnDestroy {

  @Input() seasonId = '';
  @Input() isMyTurn = false;
  @Input() isDraftActive = false;
  @Input() isLotteryRunning = false;
  @Input() currentClubName = 'Waiting...';
  @Input() currentBudget = 0;
  @Input() refreshKey = 0;

  @Output() refreshRequested = new EventEmitter<void>();
  @Output() toast = new EventEmitter<string>();

  readonly positions = [
    'All',
    'ST',
    'LW',
    'RW',
    'CM',
    'CDM',
    'CB',
    'LB',
    'RB',
    'GK'
  ];

  players: DraftPlayer[] = [];

  searchTerm = '';
  selectedPosition = 'All';

  currentPage = 1;
  pageSize = 9;

  isPicking = false;

  private searchSubject = new Subject<string>();
  private destroy$ = new Subject<void>();

  constructor(
    private draftService: DraftService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.searchSubject
      .pipe(
        debounceTime(400),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.searchPlayers();
      });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['seasonId'] || changes['refreshKey']) {
      this.loadPlayers();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get pagedPlayers(): DraftPlayer[] {
    return this.players;
  }

  get hasPlayers(): boolean {
    return this.players.length > 0;
  }

  get totalPages(): number {
    return this.players.length < this.pageSize
      ? this.currentPage
      : this.currentPage + 1;
  }

  get canGoPrevious(): boolean {
    return this.currentPage > 1;
  }

  get canGoNext(): boolean {
    return this.players.length >= this.pageSize;
  }

  get canDraft(): boolean {
    return (
      this.isDraftActive &&
      this.isMyTurn &&
      !this.isPicking &&
      !this.isLotteryRunning
    );
  }

  trackByPlayer(_: number, player: DraftPlayer): string {
    return player.gamePlayerId;
  }

  onSearchTermChanged(value: string): void {
    this.searchTerm = value;
    this.searchSubject.next(value);
  }

  searchPlayers(): void {
    this.resetPaging();
    this.loadPlayers();
  }

  reloadPlayers(): void {
    this.resetPaging();
    this.loadPlayers();
  }

  loadPlayers(): void {
    if (!this.seasonId) {
      return;
    }

    this.draftService
      .getPlayers(
        this.seasonId,
        this.searchTerm,
        this.selectedPosition === 'All'
          ? ''
          : this.selectedPosition,
        this.currentPage,
        this.pageSize
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

          this.toast.emit(
            err?.error?.message ??
            'Error loading players.'
          );
        }
      });
  }

  setPosition(position: string): void {
    if (this.selectedPosition === position) {
      return;
    }

    this.selectedPosition = position;
    this.resetPaging();
    this.loadPlayers();
  }

  nextPage(): void {
    if (!this.canGoNext) {
      return;
    }

    this.currentPage++;
    this.loadPlayers();
  }

  previousPage(): void {
    if (!this.canGoPrevious) {
      return;
    }

    this.currentPage--;
    this.loadPlayers();
  }

  draftPlayer(player: DraftPlayer): void {
    if (!this.isMyTurn) {
      this.toast.emit(
        `⏳ You're on deck. ${this.currentClubName} is currently on the clock.`
      );

      return;
    }

    if (!this.seasonId) {
      this.toast.emit('Season was not found.');
      return;
    }

    if (!this.isDraftActive) {
      this.toast.emit('The draft is not active right now.');
      return;
    }

    if (this.isLotteryRunning) {
      this.toast.emit('The draft lottery is still running.');
      return;
    }

    if (this.isPicking) {
      return;
    }

    const playerValue = player.value ?? 0;

    if (playerValue > this.currentBudget) {
      this.toast.emit(
        `❌ Not enough budget. ${player.name} costs €${playerValue.toLocaleString()} and your budget is €${this.currentBudget.toLocaleString()}.`
      );

      return;
    }

    this.isPicking = true;
    this.cdr.detectChanges();

    this.draftService
      .makePick(this.seasonId, player.gamePlayerId)
      .pipe(
        finalize(() => {
          this.isPicking = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: () => {
          this.refreshRequested.emit();
        },
        error: err => {
          console.error('Error making draft pick:', err);

          this.toast.emit(
            err?.error?.message ??
            `Could not draft ${player.name}. Please try again.`
          );
        }
      });
  }

  private resetPaging(): void {
    this.currentPage = 1;
  }

}
import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize, forkJoin, take } from 'rxjs';

import { DraftService } from '../../core/services/draft.service';
import { SeasonService } from '../../core/services/season.service';

import {
  DraftCenter,
  DraftOrder,
  DraftPick,
  DraftPlayer,
  DraftSquadPlayer
} from '../../core/models/draft.model';

interface SquadSlot {
  position: string;
  player?: DraftSquadPlayer;
}

@Component({
  selector: 'app-draft',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './draft.html',
  styleUrl: './draft.scss'
})
export class Draft implements OnInit, OnDestroy {
  isLoading = true;
  isPicking = false;
  errorMessage = '';

  searchTerm = '';
  selectedPosition = 'All';
  currentPage = 1;
  pageSize = 9;

  secondsRemaining = 0;
  timerText = '00:00:00';

  draftCenter: DraftCenter | null = null;

  draftOrder: DraftOrder[] = [];
  players: DraftPlayer[] = [];
  lastPicks: DraftPick[] = [];

  seasonId = '';

  private timerId?: number;

  readonly playerImageFallback = 'https://cdn.sofifa.net/players/239/085/26_120.png';

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
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.route.queryParamMap
      .pipe(take(1))
      .subscribe(params => {
        this.seasonId = params.get('seasonId') ?? '';

        if (this.seasonId) {
          this.loadDraft();
          return;
        }

        this.loadCurrentSeasonAndDraft();
      });
  }

  ngOnDestroy(): void {
    if (this.timerId) {
      window.clearInterval(this.timerId);
    }
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
    return this.draftCenter?.state.currentRound ?? 1;
  }

  get playersPerTeam(): number {
    return this.draftCenter?.state.playersPerTeam ?? this.squadSlots.length;
  }

  get totalTeams(): number {
    return this.draftCenter?.state.totalTeams ?? 0;
  }

  get currentPickNumber(): number {
    return this.draftCenter?.state.currentPickNumber ?? 1;
  }

  get currentClubName(): string {
    return this.draftCenter?.state.currentClubName || 'Waiting...';
  }

  get isDraftActive(): boolean {
    return this.draftCenter?.state.isActive ?? false;
  }

  getPlayerImageUrl(player: DraftPlayer): string {
    return player.imageUrl?.trim() || this.playerImageFallback;
  }

  onPlayerImageError(event: Event): void {
    const img = event.target as HTMLImageElement;

    if (img.src !== this.playerImageFallback) {
      img.src = this.playerImageFallback;
    }
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
        this.pageSize
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

          this.mapMySquadToSlots();
          this.startTimer(center.state.secondsRemaining);

          this.cdr.detectChanges();
        },
        error: err => {
          console.error('Error loading draft:', err);
          this.errorMessage = err?.error?.message || 'Error loading draft.';
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
    this.errorMessage =
      `⏳ You're on deck. ${this.currentClubName} is currently on the clock.`;

    this.cdr.detectChanges();
    return;
  }

  if (!this.seasonId) {
    this.errorMessage = 'Season was not found.';
    this.cdr.detectChanges();
    return;
  }

  if (!this.isDraftActive) {
    this.errorMessage = 'The draft is not active right now.';
    this.cdr.detectChanges();
    return;
  }

  if (this.isPicking) {
    return;
  }

  const playerValue = player.value ?? 0;

  if (playerValue > this.currentBudget) {

  this.errorMessage =
    `❌ Not enough budget. ${player.name} costs €${playerValue.toLocaleString()} and your budget is €${this.currentBudget.toLocaleString()}.`;

  this.cdr.detectChanges();

  setTimeout(() => {
    this.errorMessage = '';
    this.cdr.detectChanges();
  }, 3500);

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
      },
      error: err => {
        console.error('Error making draft pick:', err);

        this.errorMessage =
          err?.error?.message ||
          `Could not draft ${player.name}. Please try again.`;

        this.cdr.detectChanges();
      }
    });
}

private showToast(message: string): void {
  this.errorMessage = message;
  this.cdr.detectChanges();

  setTimeout(() => {
    this.errorMessage = '';
    this.cdr.detectChanges();
  }, 3500);
}

get currentBudget(): number {
  return this.draftCenter?.currentBudget ?? 0;
}

  private mapMySquadToSlots(): void {
    this.squadSlots = this.squadSlots.map(slot => ({
      position: slot.position
    }));

    const squadPlayers = this.draftCenter?.mySquad.players ?? [];

    squadPlayers.forEach(player => {
      const slot =
        this.squadSlots.find(x => !x.player && x.position === player.position) ??
        this.squadSlots.find(x => !x.player);

      if (slot) {
        slot.player = player;
      }
    });
  }

  private startTimer(seconds: number): void {
    if (this.timerId) {
      window.clearInterval(this.timerId);
    }

    this.secondsRemaining = Math.max(0, seconds);
    this.timerText = this.formatTime(this.secondsRemaining);
    this.cdr.detectChanges();

    this.timerId = window.setInterval(() => {
      if (this.secondsRemaining <= 0) {
        window.clearInterval(this.timerId);
        this.timerText = '00:00:00';
        this.cdr.detectChanges();
        return;
      }

      this.secondsRemaining--;
      this.timerText = this.formatTime(this.secondsRemaining);
      this.cdr.detectChanges();
    }, 1000);
  }

  private formatTime(totalSeconds: number): string {
    const safeSeconds = Math.max(0, totalSeconds);
    const minutes = Math.floor(safeSeconds / 60);
    const seconds = safeSeconds % 60;

    return `00:${minutes.toString().padStart(2, '0')}:${seconds
      .toString()
      .padStart(2, '0')}`;
  }

  getPrimaryPosition(position: string): string {
  if (!position) {
    return '';
  }

  return position.split(',')[0].trim();
}
get isMyTurn(): boolean {
  if (!this.draftCenter) {
    return false;
  }

  return this.draftCenter.myUserId ===
         this.draftCenter.state.currentUserId;
}
}
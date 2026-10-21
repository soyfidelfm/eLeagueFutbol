import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subscription, finalize, forkJoin } from 'rxjs';

import { RankingService } from '../../core/services/ranking.service';
import { SeasonService } from '../../core/services/season.service';
import { RealtimeService } from '../../core/services/realtime.service';

import { ClubRankingDto, PlayerRankingDto } from '../../core/models/ranking.model';
import { Season } from '../../core/models/season.model';
import { AppLoading } from '../../shared/components/app-loading/app-loading';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { NeonDropdownComponent, NeonDropdownOption } from '../../shared/components/neon-dropdown/neon-dropdown';

type RankingSort = 'points' | 'goalDifference' | 'goalsFor' | 'name';

@Component({
  selector: 'app-rankings',
  standalone: true,
  imports: [CommonModule, FormsModule, AppLoading,
    TranslatePipe, EmptyState, NeonDropdownComponent
  ],
  templateUrl: './rankings.html',
  styleUrl: './rankings.scss',
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
  `]
})
export class Rankings implements OnInit, OnDestroy {

  selectedSeasonId = '';

  seasons: Season[] = [];
  clubRankings: ClubRankingDto[] = [];
  playerRankings: PlayerRankingDto[] = [];

  // Client-side filters/sort. Kept as component state so they survive SignalR reloads.
  searchTerm = '';
  clubFilter = 'all';
  sortBy: RankingSort = 'points';

  readonly sortOptions: NeonDropdownOption[] = [
    { label: 'Points', value: 'points' },
    { label: 'Goal difference', value: 'goalDifference' },
    { label: 'Goals for', value: 'goalsFor' },
    { label: 'Name (A–Z)', value: 'name' }
  ];

  get clubOptions(): NeonDropdownOption[] {
    const clubs = [...this.clubRankings].sort((a, b) => a.clubName.localeCompare(b.clubName));
    return [{ label: 'All clubs', value: 'all' }, ...clubs.map(c => ({ label: c.clubName, value: c.clubId }))];
  }

  get hasActiveFilters(): boolean {
    return !!this.searchTerm.trim() || this.clubFilter !== 'all';
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.clubFilter = 'all';
  }

  onSortChange(value: string): void {
    this.sortBy = value as RankingSort;
  }

  get filteredClubRankings(): ClubRankingDto[] {
    const term = this.searchTerm.trim().toLowerCase();
    const playerClubIds = new Set(
      term
        ? this.playerRankings
            .filter(p => (p.playerName ?? '').toLowerCase().includes(term) && p.clubId)
            .map(p => p.clubId as string)
        : []
    );

    const list = this.clubRankings.filter(c => {
      if (this.clubFilter !== 'all' && c.clubId !== this.clubFilter) return false;
      if (term && !c.clubName.toLowerCase().includes(term) && !playerClubIds.has(c.clubId)) return false;
      return true;
    });

    const byRank = (a: ClubRankingDto, b: ClubRankingDto) => a.rank - b.rank;
    switch (this.sortBy) {
      case 'goalDifference':
        return list.sort((a, b) => b.goalDifference - a.goalDifference || byRank(a, b));
      case 'goalsFor':
        return list.sort((a, b) => b.goalsFor - a.goalsFor || byRank(a, b));
      case 'name':
        return list.sort((a, b) => a.clubName.localeCompare(b.clubName));
      default:
        return list.sort((a, b) => b.points - a.points || byRank(a, b));
    }
  }

  isLoading = false;
  isRefreshing = false;
  errorMessage = '';
  liveMessage = '';

  private joinedSeasonId = '';
  private subscription = new Subscription();

  constructor(
    private rankingService: RankingService,
    private seasonService: SeasonService,
    private realtime: RealtimeService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.loadInitialData();

    this.subscription.add(
      this.realtime.draftEvents$.subscribe(event => {
        if ((event?.eventType ?? '').toLowerCase() !== 'matchresult') {
          return;
        }

        if (event.seasonId && this.selectedSeasonId && event.seasonId !== this.selectedSeasonId) {
          return;
        }

        this.liveMessage = 'Standings updated';
        this.loadRankings(false);
      })
    );
  }

  ngOnDestroy(): void {
    if (this.joinedSeasonId) {
      void this.realtime.leaveSeason(this.joinedSeasonId);
    }

    this.subscription.unsubscribe();
  }

  loadInitialData(): void {
    this.isLoading = true;
    this.errorMessage = '';

    forkJoin({
      seasons: this.seasonService.getAll(),
      rankings: this.rankingService.getRankings()
    }).subscribe({
      next: ({ seasons, rankings }) => {
        this.seasons = seasons ?? [];

        this.selectedSeasonId =
          rankings?.seasonId ||
          this.seasons.find(x => x.isCurrent)?.seasonId ||
          this.seasons[0]?.seasonId ||
          '';

        this.clubRankings = rankings?.clubRankings ?? [];
          this.playerRankings = rankings?.playerRankings ?? [];
        void this.joinSelectedSeason();

        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error loading rankings:', error);

        this.errorMessage = 'Unable to load rankings.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  onSeasonChange(): void {
    if (!this.selectedSeasonId) {
      return;
    }

    // Club ids differ per season; keep search + sort, reset the club filter.
    this.clubFilter = 'all';
    void this.joinSelectedSeason();
    this.loadRankings(true);
  }

  get selectedSeasonName(): string {
    return this.seasons.find(x => x.seasonId === this.selectedSeasonId)?.name ?? 'Season';
  }

  getTotalGoals(): number {
    return this.clubRankings.reduce(
      (total, club) => total + club.goalsFor,
      0
    );
  }

  getRankClass(rank: number): string {
    if (rank === 1) return 'gold';
    if (rank === 2) return 'silver';
    if (rank === 3) return 'bronze';
    return '';
  }

  private loadRankings(showLoader: boolean): void {
    if (!this.selectedSeasonId) {
      return;
    }

    if (showLoader) {
      this.isLoading = true;
    } else {
      this.isRefreshing = true;
    }

    this.errorMessage = '';

    this.rankingService.getRankings(this.selectedSeasonId)
      .pipe(finalize(() => {
        this.isLoading = false;
        this.isRefreshing = false;
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (rankings) => {
          this.clubRankings = rankings?.clubRankings ?? [];
          this.playerRankings = rankings?.playerRankings ?? [];
        },
        error: (error) => {
          console.error('Error loading rankings by season:', error);
          this.errorMessage = 'Unable to load rankings for this season.';
        }
      });
  }

  private async joinSelectedSeason(): Promise<void> {
    if (!this.selectedSeasonId || this.selectedSeasonId === this.joinedSeasonId) {
      return;
    }

    if (this.joinedSeasonId) {
      await this.realtime.leaveSeason(this.joinedSeasonId);
    }

    await this.realtime.joinSeason(this.selectedSeasonId);
    this.joinedSeasonId = this.selectedSeasonId;
  }
}

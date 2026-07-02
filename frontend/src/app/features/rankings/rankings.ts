import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';

import { RankingService } from '../../core/services/ranking.service';
import { SeasonService } from '../../core/services/season.service';

import { ClubRankingDto } from '../../core/models/ranking.model';
import { Season } from '../../core/models/season.model';
import { AppLoading } from '../../shared/components/app-loading/app-loading';

@Component({
  selector: 'app-rankings',
  standalone: true,
  imports: [CommonModule, FormsModule, AppLoading],
  templateUrl: './rankings.html',
  styleUrl: './rankings.scss'
})
export class Rankings implements OnInit {

  selectedSeasonId = '';

  seasons: Season[] = [];
  clubRankings: ClubRankingDto[] = [];

  isLoading = false;
  errorMessage = '';

  constructor(
    private rankingService: RankingService,
    private seasonService: SeasonService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.loadInitialData();
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

    this.isLoading = true;
    this.errorMessage = '';

    this.rankingService.getRankings(this.selectedSeasonId).subscribe({
      next: (rankings) => {
        this.clubRankings = rankings?.clubRankings ?? [];

        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error loading rankings by season:', error);

        this.errorMessage = 'Unable to load rankings for this season.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
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
}
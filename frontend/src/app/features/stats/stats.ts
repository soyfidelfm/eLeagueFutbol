import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { StatsService } from '../../core/services/stats.service';
import { StatsOverview } from '../../core/models/stats-overview.model';
import { AppLoading } from '../../shared/components/app-loading/app-loading';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { EmptyState } from '../../shared/components/empty-state/empty-state';

@Component({
  selector: 'app-stats',
  standalone: true,
  imports: [CommonModule, RouterLink, AppLoading,
    TranslatePipe, EmptyState
  ],
  templateUrl: './stats.html',
  styleUrl: './stats.scss',
})
export class Stats implements OnInit {

  isLoading = false;
  errorMessage = '';

  stats: StatsOverview | null = null;

  selectedTab = 'overview';

  /** Highest goals/assists value in the performance series (min 1) used to scale bars. */
  perfMax = 1;

  constructor(
    private statsService: StatsService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadStats();
  }

  setTab(tab: string): void {
    this.selectedTab = tab;
  }

  /** Bar height as a percentage of the chart, scaled to the max value in the series. */
  barHeight(value: number): number {
    return Math.max(0, ((value ?? 0) / this.perfMax) * 100);
  }

  loadStats(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.stats = null;

    this.cdr.detectChanges();

    this.statsService.getOverview()
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: response => {
          this.stats = { ...response, performance: response?.performance ?? [] };
          this.perfMax = Math.max(
            1,
            ...this.stats.performance.map(p => Math.max(p.goals ?? 0, p.assists ?? 0))
          );
          this.cdr.detectChanges();
        },
        error: err => {
          console.error('Error loading stats:', err);
          this.errorMessage = 'Could not load stats.';
          this.cdr.detectChanges();
        }
      });
  }
}

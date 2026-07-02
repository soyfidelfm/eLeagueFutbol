import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { finalize } from 'rxjs';

import { StatsService } from '../../core/services/stats.service';
import { StatsOverview } from '../../core/models/stats-overview.model';
import { AppLoading } from '../../shared/components/app-loading/app-loading';

@Component({
  selector: 'app-stats',
  standalone: true,
  imports: [CommonModule, AppLoading],
  templateUrl: './stats.html',
  styleUrl: './stats.scss',
})
export class Stats implements OnInit {

  isLoading = false;
  errorMessage = '';

  stats: StatsOverview | null = null;

  selectedTab = 'overview';

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
        console.log('Stats response:', response);

        this.stats = response;

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
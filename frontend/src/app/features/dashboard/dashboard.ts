import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { finalize, take } from 'rxjs';

import {
  DashboardService,
  DashboardResponse
} from '../../core/services/dashboard.service';

import { Activity } from '../../core/models/activity.model';
import { FeaturedPlayer } from '../../core/models/featuredplayer.model';
import { Kpi } from '../../core/models/kpi.model';
import { Match } from '../../core/models/match.model';
import { MatchOfWeek } from '../../core/models/matchofweek.model';
import { MyStats } from '../../core/models/mystats.model';
import { NewsItem } from '../../core/models/newsitem.model';
import { Player } from '../../core/models/player.model';
import { UpcomingMatch } from '../../core/models/upcomingmatch.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit {
  isLoading = false;
  errorMessage = '';

  kpis: Kpi[] = [];
  leaderboard: Player[] = [];
  matches: Match[] = [];
  upcomingMatches: UpcomingMatch[] = [];
  news: NewsItem[] = [];
  activities: Activity[] = [];

  matchOfWeek: MatchOfWeek | null = null;
  playerOfWeek: FeaturedPlayer | null = null;

  myStats: MyStats = {
    winRate: '0%',
    goals: 0,
    assists: 0,
    cleanSheets: 0,
    avgPossession: '0%'
  };

  constructor(
    private dashboardService: DashboardService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadDashboard();
  }

  private loadDashboard(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.dashboardService
      .getDashboard()
      .pipe(
        take(1),
        finalize(() => {
          console.log('Dashboard finalize');
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (response: DashboardResponse) => {
          console.log('Dashboard response:', response);

          this.kpis = response.kpis ?? [];
          this.leaderboard = response.leaderboard ?? [];
          this.matchOfWeek = response.matchOfWeek ?? null;
          this.matches = response.matches ?? [];
          this.upcomingMatches = response.upcomingMatches ?? [];
          this.news = response.news ?? [];
          this.playerOfWeek = response.playerOfWeek ?? null;
          this.myStats = response.myStats ?? this.myStats;
          this.activities = response.activities ?? [];
        },
        error: error => {
          console.error('Dashboard load failed', error);
          this.errorMessage = 'Could not load dashboard.';
        }
      });
  }
}
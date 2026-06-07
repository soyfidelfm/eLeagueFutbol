import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { finalize, take } from 'rxjs';
import { DashboardResponse } from '../../core/models/dashboard-response.model';
import {
  DashboardService  
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

import { SeasonService } from '../../core/services/season.service';
import { FormsModule } from '@angular/forms';

interface CurrentSeasonView {
  seasonId: string;
  name: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule,FormsModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit {
  showRegisterModal = false;
registerMessage = '';


  isLoading = false;
  errorMessage = '';

  currentSeason: CurrentSeasonView | null = null;

  isRegistered = false;
  isRegistering = false;

  selectedGameClubId = '';

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
    private seasonService: SeasonService,
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
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (response: DashboardResponse) => {
  console.log('Dashboard response:', response);

  this.currentSeason =
    response.currentSeasonId &&
    response.currentSeasonName
      ? {
          seasonId: response.currentSeasonId,
          name: response.currentSeasonName
        }
      : null;

  this.kpis = response.kpis ?? [];
  this.leaderboard = response.leaderboard ?? [];
  this.matchOfWeek = response.matchOfWeek ?? null;
  this.matches = response.matches ?? [];
  this.upcomingMatches = response.upcomingMatches ?? [];
  this.news = response.news ?? [];
  this.playerOfWeek = response.playerOfWeek ?? null;
  this.myStats = response.myStats ?? this.myStats;
  this.activities = response.activities ?? [];

  console.log('Current season:', this.currentSeason);

  if (this.currentSeason?.seasonId) {
    this.checkRegistration();
  }
},
        error: error => {
          console.error('Dashboard load failed', error);
          this.errorMessage = 'Could not load dashboard.';
        }
      });
  }

  private setCurrentSeason(response: DashboardResponse): void {
    const responseAny = response as any;

    if (responseAny.currentSeasonId && responseAny.currentSeasonName) {
      this.currentSeason = {
        seasonId: responseAny.currentSeasonId,
        name: responseAny.currentSeasonName
      };

      return;
    }

    const seasonKpi = this.kpis.find(
      x => x.label?.toLowerCase() === 'season'
    );

    if (seasonKpi?.value && seasonKpi.value !== 'No Season') {
      this.currentSeason = {
        seasonId: '',
        name: seasonKpi.value
      };

      return;
    }

    this.currentSeason = null;
  }

  checkRegistration(): void {
    if (!this.currentSeason?.seasonId) {
      return;
    }

    this.seasonService
      .isRegistered(this.currentSeason.seasonId)
      .pipe(take(1))
      .subscribe({
        next: result => {
          this.isRegistered = result;
          this.cdr.detectChanges();
        },
        error: err => {
          console.error('Error checking registration', err);
        }
      });
  }

  registerSeason(): void {
    if (!this.currentSeason?.seasonId) {
      console.warn('Missing current season id');
      return;
    }

    if (!this.selectedGameClubId) {
      console.warn('Missing game club');
      return;
    }

    this.isRegistering = true;

    this.seasonService
      .register(this.currentSeason.seasonId, {
        gameClubId: this.selectedGameClubId
      })
      .pipe(
        take(1),
        finalize(() => {
          this.isRegistering = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: () => {
          this.isRegistered = true;
        },
        error: err => {
          console.error('Error registering season', err);
        }
      });
  }

  openRegisterModal(): void {
  if (!this.currentSeason?.seasonId) {
    console.warn('Missing current season id');
    return;
  }

  this.registerMessage = '';
  this.showRegisterModal = true;
}

closeRegisterModal(): void {
  this.showRegisterModal = false;
}

confirmRegistration(): void {
  if (!this.currentSeason?.seasonId) {
    return;
  }

  if (!this.selectedGameClubId) {
    this.registerMessage = 'Select your club first.';
    return;
  }

  this.isRegistering = true;
  this.registerMessage = '';

  this.seasonService
    .register(this.currentSeason.seasonId, {
      gameClubId: this.selectedGameClubId
    })
    .pipe(
      take(1),
      finalize(() => {
        this.isRegistering = false;
        this.cdr.detectChanges();
      })
    )
    .subscribe({
      next: () => {
        this.isRegistered = true;
        this.showRegisterModal = false;
      },
      error: err => {
        console.error('Error registering season', err);
        this.registerMessage =
          err?.error?.message ?? 'Could not register.';
      }
    });
}
}
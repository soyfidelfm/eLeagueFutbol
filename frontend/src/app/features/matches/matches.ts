import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  OnInit
} from '@angular/core';
import { finalize, forkJoin } from 'rxjs';

import { MatchService } from '../../core/services/match.service';
import { SeasonService } from '../../core/services/season.service';

import {
  MatchStatsService,
  MatchStatsUploadResult
} from '../../core/services/match-stats.service';

import { AppLoading } from '../../shared/components/app-loading/app-loading';

import {
  MatchDto,
  MatchWeekDto
} from '../../core/models/match.model';

import { MatchView } from '../../core/models/match-view.model';

import {
  NeonDropdownComponent,
  NeonDropdownOption
} from '../../shared/components/neon-dropdown/neon-dropdown';

import {
  ManualMatchStatsModal
} from './components/manual-match-stats-modal/manual-match-stats-modal';

@Component({
  selector: 'app-matches',
  standalone: true,
  imports: [
    CommonModule,
    NeonDropdownComponent,
    AppLoading,
    ManualMatchStatsModal
  ],
  templateUrl: './matches.html',
  styleUrl: './matches.scss'
})
export class Matches implements OnInit {

  summaryFile: File | null = null;
  homePerformanceFile: File | null = null;
  awayPerformanceFile: File | null = null;

  manualStatsMatch: MatchView | null = null;

  isLoading = false;
  isUploadingResult = false;
  errorMessage = '';

  selectedSeasonId = '';
  selectedWeek = 1;

  weeks: MatchWeekDto[] = [];
  weekOptions: NeonDropdownOption[] = [];

  matches: MatchView[] = [];
  selectedMatch: MatchView | null = null;

  selectedResultFiles: File[] = [];
  statsUploadResult: MatchStatsUploadResult | null = null;

  constructor(
    private readonly matchService: MatchService,
    private readonly seasonService: SeasonService,
    private readonly matchStatsService: MatchStatsService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadCurrentSeason();
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
          seasons.find(x => x.isCurrent) ??
          seasons[0];

        if (!currentSeason) {
          this.errorMessage = 'No season found.';
          this.isLoading = false;
          this.cdr.detectChanges();
          return;
        }

        this.selectedSeasonId = currentSeason.seasonId;
        this.loadWeeks();
      },
      error: () => {
        this.errorMessage = 'Could not load season.';
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

          this.selectedWeek = Number(weeks[0].value);
          this.loadMatches();
        },
        error: () => {
          this.errorMessage = 'Could not load match weeks.';
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      });
  }

  loadMatches(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.matches = [];
    this.selectedMatch = null;
    this.clearUploadState();
    this.cdr.detectChanges();

    this.matchService
      .getByWeek(
        this.selectedSeasonId,
        Number(this.selectedWeek)
      )
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: matches => {
          this.matches = matches.map(match =>
            this.mapMatch(match)
          );

          this.selectedMatch =
            this.matchOfTheWeek ??
            this.matches[0] ??
            null;

          this.cdr.detectChanges();
        },
        error: () => {
          this.errorMessage = 'Could not load matches.';
          this.matches = [];
          this.selectedMatch = null;
          this.cdr.detectChanges();
        }
      });
  }

  onWeekDropdownChange(value: string): void {
    this.selectedWeek = Number(value);
    this.loadMatches();
  }

  selectMatch(match: MatchView): void {
    this.selectedMatch = match;
    this.clearUploadState();
  }

  previewUploadedResult(): void {
    if (!this.selectedMatch) {
      this.errorMessage = 'Please select a match.';
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

    forkJoin([
      this.matchStatsService.uploadStatsImage(
        this.selectedMatch.id,
        'MatchSummary',
        this.summaryFile
      ),
      this.matchStatsService.uploadStatsImage(
        this.selectedMatch.id,
        'HomeTeamPerformance',
        this.homePerformanceFile
      ),
      this.matchStatsService.uploadStatsImage(
        this.selectedMatch.id,
        'AwayTeamPerformance',
        this.awayPerformanceFile
      )
    ])
      .pipe(
        finalize(() => {
          this.isUploadingResult = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: results => {
          this.statsUploadResult = results[0];
          this.loadMatches();
        },
        error: err => {
          console.error(err);

          this.errorMessage =
            'Could not upload match stats.';

          this.statsUploadResult = null;
          this.cdr.detectChanges();
        }
      });
  }

  confirmUploadedResult(): void {
    this.previewUploadedResult();
  }

  onSummarySelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.summaryFile = input.files?.[0] ?? null;
  }

  onHomePerformanceSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.homePerformanceFile =
      input.files?.[0] ?? null;
  }

  onAwayPerformanceSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.awayPerformanceFile =
      input.files?.[0] ?? null;
  }

  openManualStats(): void {
    if (!this.selectedMatch) {
      this.errorMessage = 'Please select a match.';
      return;
    }

    this.errorMessage = '';
    this.manualStatsMatch = this.selectedMatch;
    this.cdr.detectChanges();
  }

  closeManualStats(): void {
    this.manualStatsMatch = null;
    this.cdr.detectChanges();
  }

  onManualStatsSaved(): void {
    this.manualStatsMatch = null;
    this.loadMatches();
  }

  get selectedWeekValue(): string {
    return this.selectedWeek.toString();
  }

  get filteredMatches(): MatchView[] {
    return this.matches;
  }

  get matchOfTheWeek(): MatchView | null {
    return (
      this.matches.find(x => x.isMatchOfTheWeek) ??
      this.matches[0] ??
      null
    );
  }

  get regularMatches(): MatchView[] {
    const matchOfTheWeek = this.matchOfTheWeek;

    if (!matchOfTheWeek) {
      return this.matches;
    }

    return this.matches.filter(
      x => x.id !== matchOfTheWeek.id
    );
  }

  private clearUploadState(): void {
    this.summaryFile = null;
    this.homePerformanceFile = null;
    this.awayPerformanceFile = null;

    this.selectedResultFiles = [];
    this.statsUploadResult = null;
  }

  private mapMatch(match: MatchDto): MatchView {
    return {
      id: match.matchId,
      weekNumber: match.weekNumber,
      isMatchOfTheWeek: match.isMatchOfTheWeek,

      arena: match.arena,
      mode: match.mode,
      date: match.matchDate,
      time: match.matchTime,

      homeScore: match.homeScore,
      awayScore: match.awayScore,

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
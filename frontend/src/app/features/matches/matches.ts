import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { finalize } from 'rxjs';

import { MatchService } from '../../core/services/match.service';
import { SeasonService } from '../../core/services/season.service';
import { MatchStatsService } from '../../core/services/match-stats.service';
import { MatchOcrPreview } from '../../core/models/match-player-stats-ocr.model';

import { AppLoading } from '../../shared/components/app-loading/app-loading';

import {
  MatchDto,
  MatchWeekDto,
  UploadMatchResultDto
} from '../../core/models/match.model';

import { MatchView } from '../../core/models/match-view.model';

import {
  NeonDropdownComponent,
  NeonDropdownOption
} from '../../shared/components/neon-dropdown/neon-dropdown';

@Component({
  selector: 'app-matches',
  standalone: true,
  imports: [
    CommonModule,
    NeonDropdownComponent,
    AppLoading
  ],
  templateUrl: './matches.html',
  styleUrl: './matches.scss'
})
export class Matches implements OnInit {

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
  ocrPreview: MatchOcrPreview | null = null;

  constructor(
    private matchService: MatchService,
    private seasonService: SeasonService,
    private matchStatsService: MatchStatsService,
    private cdr: ChangeDetectorRef
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

    this.seasonService.getAll()
      .subscribe({
        next: seasons => {
          const currentSeason = seasons.find(x => x.isCurrent) ?? seasons[0];

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
    this.matchService.getWeeks(this.selectedSeasonId)
      .subscribe({
        next: weeks => {
          this.weeks = weeks;

          this.weekOptions = this.weeks.map(week => ({
            label: week.label,
            value: week.value.toString()
          }));

          if (this.weeks.length === 0) {
            this.matches = [];
            this.selectedMatch = null;
            this.isLoading = false;
            this.cdr.detectChanges();
            return;
          }

          this.selectedWeek = Number(this.weeks[0].value);
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

    this.matchService.getByWeek(this.selectedSeasonId, Number(this.selectedWeek))
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: matches => {
          this.matches = matches.map(match => this.mapMatch(match));
          this.selectedMatch = this.matchOfTheWeek ?? this.matches[0] ?? null;
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

  onResultFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;

    if (!input.files || input.files.length === 0) {
      return;
    }

    this.selectedResultFiles = Array.from(input.files);
    this.ocrPreview = null;
    this.errorMessage = '';
  }

  previewUploadedResult(): void {
    if (!this.selectedMatch || this.selectedResultFiles.length === 0) {
      this.errorMessage = 'Please select Summary and Events screenshots.';
      return;
    }

    this.isUploadingResult = true;
    this.errorMessage = '';

    this.matchStatsService.previewMatchScreenshots(this.selectedResultFiles)
      .pipe(
        finalize(() => {
          this.isUploadingResult = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: result => {
          this.ocrPreview = result;

          if (result.homeScore !== null && result.homeScore !== undefined) {
            this.selectedMatch!.homeScore = result.homeScore;
          }

          if (result.awayScore !== null && result.awayScore !== undefined) {
            this.selectedMatch!.awayScore = result.awayScore;
          }

          this.cdr.detectChanges();
        },
        error: () => {
          this.errorMessage = 'Could not read match screenshots.';
          this.ocrPreview = null;
          this.cdr.detectChanges();
        }
      });
  }

  confirmUploadedResult(): void {
    if (!this.selectedMatch || !this.ocrPreview) {
      return;
    }

    const dto: UploadMatchResultDto = {
      homeScore: this.ocrPreview.homeScore ?? 0,
      awayScore: this.ocrPreview.awayScore ?? 0
    };

    this.isUploadingResult = true;

    this.matchService.uploadResult(
      this.selectedMatch.id,
      dto
    )
    .pipe(
      finalize(() => {
        this.isUploadingResult = false;
        this.cdr.detectChanges();
      })
    )
    .subscribe({
      next: () => {
        this.selectedResultFiles = [];
        this.ocrPreview = null;
        this.loadMatches();
      },
      error: () => {
        this.errorMessage = 'Could not save match result.';
      }
    });
  }

  get selectedWeekValue(): string {
    return this.selectedWeek.toString();
  }

  get filteredMatches(): MatchView[] {
    return this.matches;
  }

  get matchOfTheWeek(): MatchView | null {
    return this.matches.find(x => x.isMatchOfTheWeek) ?? this.matches[0] ?? null;
  }

  get regularMatches(): MatchView[] {
    const matchOfTheWeek = this.matchOfTheWeek;

    if (!matchOfTheWeek) {
      return this.matches;
    }

    return this.matches.filter(x => x.id !== matchOfTheWeek.id);
  }

  private clearUploadState(): void {
    this.selectedResultFiles = [];
    this.ocrPreview = null;
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
import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';

import { MatchService } from '../../core/services/match.service';
import { SeasonService } from '../../core/services/season.service';

import {
  MatchDto,
  MatchWeekDto,
  UploadMatchResultDto
} from '../../core/models/match.model';
import { MatchView } from '../../core/models/match-view.model';

@Component({
  selector: 'app-matches',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './matches.html',
  styleUrl: './matches.scss'
})
export class Matches implements OnInit {

  isLoading = false;
  errorMessage = '';

  selectedSeasonId = '';
  selectedWeek = 1;

  weeks: MatchWeekDto[] = [];
  matches: MatchView[] = [];

  selectedMatch: MatchView | null = null;

  constructor(
  private matchService: MatchService,
  private seasonService: SeasonService,
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
        this.cdr.detectChanges();

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

        if (this.weeks.length === 0) {
          this.matches = [];
          this.selectedMatch = null;
          this.isLoading = false;
          this.cdr.detectChanges();
          return;
        }

        this.selectedWeek = Number(this.weeks[0].value);
        this.cdr.detectChanges();

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

 onWeekChange(): void {
  this.selectedWeek = Number(this.selectedWeek);
  this.loadMatches();
}

  selectMatch(match: MatchView): void {
    this.selectedMatch = match;
  }

  uploadResult(): void {
    if (!this.selectedMatch) {
      return;
    }

    const dto: UploadMatchResultDto = {
      homeScore: this.selectedMatch.homeScore ?? 0,
      awayScore: this.selectedMatch.awayScore ?? 0
    };

    this.matchService.uploadResult(this.selectedMatch.id, dto)
      .subscribe({
        next: () => {
          this.loadMatches();
        },
        error: () => {
          this.errorMessage = 'Could not upload match result.';
        }
      });
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
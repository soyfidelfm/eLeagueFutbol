import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  EventEmitter,
  inject,
  Input,
  OnChanges,
  OnInit,
  Output,
  SimpleChanges
} from '@angular/core';
import {
  FormArray,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { finalize } from 'rxjs';

import { MatchService } from '../../../../core/services/match.service';

import {
  SaveManualMatchStatsRequest
} from '../../../../core/models/match.model';

import {
  MatchPlayerStatsOcrPreview,
  MatchSummaryStatsOcrPreview,
  TeamPerformanceOcrPreview
} from '../../../../core/models/match-stats.model';

@Component({
  selector: 'app-manual-match-stats-modal',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule
  ],
  templateUrl: './manual-match-stats-modal.html',
  styleUrl: './manual-match-stats-modal.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ManualMatchStatsModal
  implements OnInit, OnChanges {

  private readonly fb = inject(FormBuilder);
  private readonly matchService = inject(MatchService);
  private readonly cdr = inject(ChangeDetectorRef);

  @Input({ required: true }) matchId = '';

  @Input() homeTeamName = 'Home';
  @Input() awayTeamName = 'Away';

  @Input() homeGoals = 0;
  @Input() awayGoals = 0;

  @Input()
  summaryOcrPreview: MatchSummaryStatsOcrPreview | null = null;

  @Input()
  homePerformanceOcrPreview: TeamPerformanceOcrPreview | null = null;

  @Input()
  awayPerformanceOcrPreview: TeamPerformanceOcrPreview | null = null;

  @Output() closed = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  isSaving = false;
  errorMessage = '';

  readonly form = this.fb.group({
    homeGoals: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    awayGoals: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    homePossession: [
      50,
      [
        Validators.required,
        Validators.min(0),
        Validators.max(100)
      ]
    ],

    awayPossession: [
      50,
      [
        Validators.required,
        Validators.min(0),
        Validators.max(100)
      ]
    ],

    homeShots: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    awayShots: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    homeExpectedGoals: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    awayExpectedGoals: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    homePasses: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    awayPasses: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    homeTackles: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    awayTackles: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    homeTacklesWon: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    awayTacklesWon: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    homeInterceptions: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    awayInterceptions: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    homeSaves: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    awaySaves: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    homeFoulsCommitted: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    awayFoulsCommitted: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    homeOffsides: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    awayOffsides: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    homeCorners: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    awayCorners: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    homeFreeKicks: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    awayFreeKicks: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    homePenaltyKicks: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    awayPenaltyKicks: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    homeYellowCards: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    awayYellowCards: [
      0,
      [
        Validators.required,
        Validators.min(0)
      ]
    ],

    homePlayers: this.fb.array([]),
    awayPlayers: this.fb.array([])
  });

  ngOnInit(): void {
    this.populateSummaryForm();
    this.populatePlayerForms();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (
      changes['summaryOcrPreview'] ||
      changes['homeGoals'] ||
      changes['awayGoals']
    ) {
      this.populateSummaryForm();
    }

    if (
      changes['homePerformanceOcrPreview'] ||
      changes['awayPerformanceOcrPreview']
    ) {
      this.populatePlayerForms();
    }
  }

  get homePlayers(): FormArray {
    return this.form.controls.homePlayers;
  }

  get awayPlayers(): FormArray {
    return this.form.controls.awayPlayers;
  }

  get ocrWarnings(): string[] {
    const warnings = [
      ...(this.summaryOcrPreview?.warnings ?? []),
      ...(this.homePerformanceOcrPreview?.warnings ?? []),
      ...(this.awayPerformanceOcrPreview?.warnings ?? [])
    ];

    return [...new Set(warnings)];
  }

  addHomePlayer(): void {
    this.homePlayers.push(
      this.createPlayerForm()
    );

    this.cdr.markForCheck();
  }

  addAwayPlayer(): void {
    this.awayPlayers.push(
      this.createPlayerForm()
    );

    this.cdr.markForCheck();
  }

  removeHomePlayer(index: number): void {
    this.homePlayers.removeAt(index);

    this.cdr.markForCheck();
  }

  removeAwayPlayer(index: number): void {
    this.awayPlayers.removeAt(index);

    this.cdr.markForCheck();
  }

  save(): void {
    if (!this.matchId) {
      this.errorMessage = 'Match ID is required.';
      return;
    }

    if (this.form.invalid || this.isSaving) {
      this.form.markAllAsTouched();
      this.cdr.markForCheck();
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    const dto =
      this.form.getRawValue() as SaveManualMatchStatsRequest;

    this.matchService
      .saveManualStats(
        this.matchId,
        dto
      )
      .pipe(
        finalize(() => {
          this.isSaving = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: () => {
          this.saved.emit();
          this.closed.emit();
        },
        error: error => {
          console.error(error);

          this.errorMessage =
            error?.error?.message ??
            'Could not save match statistics.';

          this.cdr.detectChanges();
        }
      });
  }

  close(): void {
    if (this.isSaving) {
      return;
    }

    this.closed.emit();
  }

  private populateSummaryForm(): void {
    const preview = this.summaryOcrPreview;

    if (!preview) {
      this.form.patchValue({
        homeGoals: this.homeGoals,
        awayGoals: this.awayGoals
      });

      this.cdr.markForCheck();
      return;
    }

    this.form.patchValue({
      homeGoals: preview.homeGoals,
      awayGoals: preview.awayGoals,

      homePossession: preview.homePossession,
      awayPossession: preview.awayPossession,

      homeShots: preview.homeShots,
      awayShots: preview.awayShots,

      homeExpectedGoals:
        preview.homeExpectedGoals,

      awayExpectedGoals:
        preview.awayExpectedGoals,

      homePasses: preview.homePasses,
      awayPasses: preview.awayPasses,

      homeTackles: preview.homeTackles,
      awayTackles: preview.awayTackles,

      homeTacklesWon:
        preview.homeTacklesWon,

      awayTacklesWon:
        preview.awayTacklesWon,

      homeInterceptions:
        preview.homeInterceptions,

      awayInterceptions:
        preview.awayInterceptions,

      homeSaves: preview.homeSaves,
      awaySaves: preview.awaySaves,

      homeFoulsCommitted:
        preview.homeFoulsCommitted,

      awayFoulsCommitted:
        preview.awayFoulsCommitted,

      homeOffsides: preview.homeOffsides,
      awayOffsides: preview.awayOffsides,

      homeCorners: preview.homeCorners,
      awayCorners: preview.awayCorners,

      homeFreeKicks:
        preview.homeFreeKicks,

      awayFreeKicks:
        preview.awayFreeKicks,

      homePenaltyKicks:
        preview.homePenaltyKicks,

      awayPenaltyKicks:
        preview.awayPenaltyKicks,

      homeYellowCards:
        preview.homeYellowCards,

      awayYellowCards:
        preview.awayYellowCards
    });

    this.cdr.markForCheck();
  }

  private populatePlayerForms(): void {
    this.homePlayers.clear();
    this.awayPlayers.clear();

    const homePlayers =
      this.homePerformanceOcrPreview?.players ?? [];

    const awayPlayers =
      this.awayPerformanceOcrPreview?.players ?? [];

    for (const player of homePlayers) {
      this.homePlayers.push(
        this.createPlayerForm(player)
      );
    }

    for (const player of awayPlayers) {
      this.awayPlayers.push(
        this.createPlayerForm(player)
      );
    }

    if (this.homePlayers.length === 0) {
      this.homePlayers.push(
        this.createPlayerForm()
      );
    }

    if (this.awayPlayers.length === 0) {
      this.awayPlayers.push(
        this.createPlayerForm()
      );
    }

    this.cdr.markForCheck();
  }

  private createPlayerForm(
    player?: MatchPlayerStatsOcrPreview
  ): FormGroup {
    return this.fb.group({
      position: [
        player?.position ?? '',
        Validators.required
      ],

      playerName: [
        player?.playerName ?? '',
        Validators.required
      ],

      rating: [
        player?.rating ?? 0,
        [
          Validators.required,
          Validators.min(0),
          Validators.max(10)
        ]
      ],

      goals: [
        player?.goals ?? 0,
        [
          Validators.required,
          Validators.min(0)
        ]
      ],

      assists: [
        player?.assists ?? 0,
        [
          Validators.required,
          Validators.min(0)
        ]
      ]
    });
  }
}
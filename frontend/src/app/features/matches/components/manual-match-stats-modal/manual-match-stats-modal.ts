import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  EventEmitter,
  inject,
  Input,
  OnInit,
  Output
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
export class ManualMatchStatsModal implements OnInit {

  private readonly fb = inject(FormBuilder);
  private readonly matchService = inject(MatchService);
  private readonly cdr = inject(ChangeDetectorRef);

  @Input({ required: true }) matchId = '';
  @Input() homeTeamName = 'Home';
  @Input() awayTeamName = 'Away';
  @Input() homeGoals = 0;
  @Input() awayGoals = 0;

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
    this.form.patchValue({
      homeGoals: this.homeGoals,
      awayGoals: this.awayGoals
    });

    this.addHomePlayer();
    this.addAwayPlayer();
  }

  get homePlayers(): FormArray {
    return this.form.controls.homePlayers;
  }

  get awayPlayers(): FormArray {
    return this.form.controls.awayPlayers;
  }

  addHomePlayer(): void {
    this.homePlayers.push(this.createPlayerForm());
  }

  addAwayPlayer(): void {
    this.awayPlayers.push(this.createPlayerForm());
  }

  removeHomePlayer(index: number): void {
    this.homePlayers.removeAt(index);
  }

  removeAwayPlayer(index: number): void {
    this.awayPlayers.removeAt(index);
  }

  save(): void {
    if (!this.matchId) {
      this.errorMessage = 'Match ID is required.';
      return;
    }

    if (this.form.invalid || this.isSaving) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    const dto =
      this.form.getRawValue() as SaveManualMatchStatsRequest;

    this.matchService
      .saveManualStats(this.matchId, dto)
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
        error: err => {
          console.error(err);

          this.errorMessage =
            err?.error?.message ??
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

  private createPlayerForm(): FormGroup {
    return this.fb.group({
      position: [
        '',
        Validators.required
      ],

      playerName: [
        '',
        Validators.required
      ],

      rating: [
        0,
        [
          Validators.required,
          Validators.min(0),
          Validators.max(10)
        ]
      ],

      goals: [
        0,
        [
          Validators.required,
          Validators.min(0)
        ]
      ],

      assists: [
        0,
        [
          Validators.required,
          Validators.min(0)
        ]
      ]
    });
  }
}
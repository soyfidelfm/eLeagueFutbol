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

import {
  MatchService
} from '../../../../core/services/match.service';

import {
  SaveManualMatchStatsRequest
} from '../../../../core/models/match.model';

import {
  MatchPlayerStatsOcrPreview,
  MatchSummaryStatsOcrPreview,
  TeamPerformanceOcrPreview
} from '../../../../core/models/match-stats.model';

import {
  TeamPlayerOption
} from '../../../../core/models/team-player-option.model';

type ManualPlayer =
  NonNullable<
    SaveManualMatchStatsRequest['homePlayers']
  >[number];

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

  @Input({ required: true })
  matchId = '';

  @Input()
  homeTeamName = 'Home';

  @Input()
  awayTeamName = 'Away';

  @Input()
  homeGoals = 0;

  @Input()
  awayGoals = 0;

  /*
   * Jugadores disponibles para cada equipo.
   * Estas listas deben venir desde el componente padre.
   */
  homeTeamPlayers: TeamPlayerOption[] = [];
  awayTeamPlayers: TeamPlayerOption[] = [];

  @Input()
  summaryOcrPreview:
    MatchSummaryStatsOcrPreview | null = null;

  @Input()
  homePerformanceOcrPreview:
    TeamPerformanceOcrPreview | null = null;

  @Input()
  awayPerformanceOcrPreview:
    TeamPerformanceOcrPreview | null = null;

  @Output()
  closed = new EventEmitter<void>();

  @Output()
  saved = new EventEmitter<void>();

  isSaving = false;
  isLoading = false;
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

    homePlayers:
      this.fb.array<FormGroup>([]),

    awayPlayers:
      this.fb.array<FormGroup>([])
  });

  ngOnInit(): void {
    this.loadMatchPlayerOptions();

    if (this.hasOcrPreview()) {
      this.populateSummaryForm();
      this.populatePlayerForms();
      return;
    }

    this.loadSavedStats();
  }
  private loadMatchPlayerOptions(): void {
    if (!this.matchId) {
      return;
    }

    this.matchService
      .getMatchPlayerOptions(this.matchId)
      .subscribe({
        next: response => {
          this.homeTeamPlayers =
            response.homeTeamPlayers ?? [];

          this.awayTeamPlayers =
            response.awayTeamPlayers ?? [];

          /*
           * Relaciona estadísticas previamente cargadas
           * por nombre con el gamePlayerId real.
           */
          this.syncPlayersWithRosters();

          console.log(
            'Match player options loaded:',
            {
              homeTeamPlayers:
                this.homeTeamPlayers,

              awayTeamPlayers:
                this.awayTeamPlayers
            }
          );

          this.cdr.markForCheck();
        },

        error: error => {
          console.error(
            'Could not load match player options:',
            error
          );

          this.homeTeamPlayers = [];
          this.awayTeamPlayers = [];

          this.errorMessage =
            error?.error?.message ??
            'Could not load team players.';

          this.cdr.markForCheck();
        }
      });
  }

  ngOnChanges(
    changes: SimpleChanges
  ): void {
    const matchChanged =
      changes['matchId'];

    if (
      matchChanged &&
      !matchChanged.firstChange &&
      this.matchId
    ) {
      this.loadMatchPlayerOptions();
    }

    const ocrChanged =
      changes['summaryOcrPreview'] ||
      changes['homePerformanceOcrPreview'] ||
      changes['awayPerformanceOcrPreview'];

    if (
      ocrChanged &&
      this.hasOcrPreview()
    ) {
      this.populateSummaryForm();
      this.populatePlayerForms();
      return;
    }

    const scoreChanged =
      changes['homeGoals'] ||
      changes['awayGoals'];

    if (
      scoreChanged &&
      !this.summaryOcrPreview &&
      !this.isLoading
    ) {
      this.form.patchValue({
        homeGoals: this.homeGoals,
        awayGoals: this.awayGoals
      });

      this.cdr.markForCheck();
    }
  }

  get homePlayers(): FormArray<FormGroup> {
    return this.form.controls.homePlayers;
  }

  get awayPlayers(): FormArray<FormGroup> {
    return this.form.controls.awayPlayers;
  }

  get ocrWarnings(): string[] {
    const warnings = [
      ...(
        this.summaryOcrPreview?.warnings ??
        []
      ),
      ...(
        this.homePerformanceOcrPreview?.warnings ??
        []
      ),
      ...(
        this.awayPerformanceOcrPreview?.warnings ??
        []
      )
    ];

    return [...new Set(warnings)];
  }

  addHomePlayer(): void {
    this.homePlayers.push(
      this.createPlayerForm(
        undefined,
        this.homeTeamPlayers
      )
    );

    this.cdr.markForCheck();
  }

  addAwayPlayer(): void {
    this.awayPlayers.push(
      this.createPlayerForm(
        undefined,
        this.awayTeamPlayers
      )
    );

    this.cdr.markForCheck();
  }

  removeHomePlayer(index: number): void {
    this.homePlayers.removeAt(index);

    this.ensurePlayerRows();
    this.cdr.markForCheck();
  }

  removeAwayPlayer(index: number): void {
    this.awayPlayers.removeAt(index);

    this.ensurePlayerRows();
    this.cdr.markForCheck();
  }

  onHomePlayerSelected(index: number): void {
    const playerForm =
      this.homePlayers.at(index);

    const gamePlayerId = String(
      playerForm
        .get('gamePlayerId')
        ?.value ?? ''
    );

    const selectedPlayer =
      this.homeTeamPlayers.find(
        player =>
          player.gamePlayerId ===
          gamePlayerId
      );

    this.applySelectedPlayer(
      playerForm,
      selectedPlayer
    );
  }

  onAwayPlayerSelected(index: number): void {
    const playerForm =
      this.awayPlayers.at(index);

    const gamePlayerId = String(
      playerForm
        .get('gamePlayerId')
        ?.value ?? ''
    );

    const selectedPlayer =
      this.awayTeamPlayers.find(
        player =>
          player.gamePlayerId ===
          gamePlayerId
      );

    this.applySelectedPlayer(
      playerForm,
      selectedPlayer
    );
  }

  isHomePlayerSelected(
    gamePlayerId: string,
    currentIndex: number
  ): boolean {
    return this.isPlayerAlreadySelected(
      this.homePlayers,
      gamePlayerId,
      currentIndex
    );
  }

  isAwayPlayerSelected(
    gamePlayerId: string,
    currentIndex: number
  ): boolean {
    return this.isPlayerAlreadySelected(
      this.awayPlayers,
      gamePlayerId,
      currentIndex
    );
  }

  save(): void {
    if (!this.matchId) {
      this.errorMessage =
        'Match ID is required.';

      this.cdr.markForCheck();
      return;
    }

    if (
      this.isSaving ||
      this.isLoading
    ) {
      return;
    }

    this.removeEmptyPlayerRows(
      this.homePlayers
    );

    this.removeEmptyPlayerRows(
      this.awayPlayers
    );

    if (this.form.invalid) {
      this.form.markAllAsTouched();

      this.errorMessage =
        'Complete all required player fields or remove incomplete player rows.';

      console.log(
        'FORM INVALID:',
        this.form.getRawValue(),
        this.form.errors
      );

      this.ensurePlayerRows();
      this.cdr.markForCheck();
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    const rawValue =
      this.form.getRawValue();

    /*
     * gamePlayerId se usa solamente en el frontend.
     *
     * Aquí construimos los jugadores manualmente para
     * continuar enviando exactamente lo que espera tu DTO.
     */
    const request: SaveManualMatchStatsRequest = {
      ...rawValue,

      homePlayers:
        this.toRequestPlayers(
          this.homePlayers
        ),

      awayPlayers:
        this.toRequestPlayers(
          this.awayPlayers
        )
    } as SaveManualMatchStatsRequest;

    console.log(
      'Saving manual statistics:',
      {
        matchId: this.matchId,
        request
      }
    );

    this.matchService
      .saveManualStats(
        this.matchId,
        request
      )
      .pipe(
        finalize(() => {
          this.isSaving = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: response => {
          console.log(
            'Manual statistics saved successfully:',
            response
          );

          this.saved.emit();
          this.closed.emit();
        },

        error: error => {
          console.error(
            'Could not save manual statistics:',
            error
          );

          this.errorMessage =
            error?.error?.message ??
            error?.message ??
            'Could not save match statistics.';

          this.ensurePlayerRows();
          this.cdr.markForCheck();
        }
      });
  }

  close(): void {
    if (
      this.isSaving ||
      this.isLoading
    ) {
      return;
    }

    this.closed.emit();
  }

  private applySelectedPlayer(
    playerForm: FormGroup,
    selectedPlayer:
      TeamPlayerOption | undefined
  ): void {
    if (!selectedPlayer) {
      playerForm.patchValue({
        playerName: '',
        position: ''
      });

      playerForm
        .get('playerName')
        ?.markAsTouched();

      playerForm
        .get('position')
        ?.markAsTouched();

      this.cdr.markForCheck();
      return;
    }

    playerForm.patchValue({
      gamePlayerId:
        selectedPlayer.gamePlayerId,

      playerName:
        selectedPlayer.name,

      position:
        selectedPlayer.position
    });

    playerForm
      .get('playerName')
      ?.markAsDirty();

    playerForm
      .get('position')
      ?.markAsDirty();

    this.cdr.markForCheck();
  }

  private isPlayerAlreadySelected(
    players: FormArray<FormGroup>,
    gamePlayerId: string,
    currentIndex: number
  ): boolean {
    if (!gamePlayerId) {
      return false;
    }

    return players.controls.some(
      (control, index) => {
        if (index === currentIndex) {
          return false;
        }

        const selectedId = String(
          control
            .get('gamePlayerId')
            ?.value ?? ''
        );

        return selectedId === gamePlayerId;
      }
    );
  }

  private hasOcrPreview(): boolean {
    return Boolean(
      this.summaryOcrPreview ||
      this.homePerformanceOcrPreview ||
      this.awayPerformanceOcrPreview
    );
  }

  private loadSavedStats(): void {
    if (!this.matchId) {
      this.setInitialValues();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.matchService
      .getManualStats(this.matchId)
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: stats => {
          this.populateSavedStats(stats);
        },

        error: error => {
          console.error(
            'Could not load saved match statistics.',
            error
          );

          this.setInitialValues();

          this.errorMessage =
            error?.error?.message ??
            'Could not load saved match statistics.';

          this.cdr.detectChanges();
        }
      });
  }

  private populateSavedStats(
    stats: SaveManualMatchStatsRequest
  ): void {
    this.form.patchValue({
      homeGoals:
        stats.homeGoals ??
        this.homeGoals,

      awayGoals:
        stats.awayGoals ??
        this.awayGoals,

      homePossession:
        stats.homePossession ?? 50,

      awayPossession:
        stats.awayPossession ?? 50,

      homeShots:
        stats.homeShots ?? 0,

      awayShots:
        stats.awayShots ?? 0,

      homeExpectedGoals:
        stats.homeExpectedGoals ?? 0,

      awayExpectedGoals:
        stats.awayExpectedGoals ?? 0,

      homePasses:
        stats.homePasses ?? 0,

      awayPasses:
        stats.awayPasses ?? 0,

      homeTackles:
        stats.homeTackles ?? 0,

      awayTackles:
        stats.awayTackles ?? 0,

      homeTacklesWon:
        stats.homeTacklesWon ?? 0,

      awayTacklesWon:
        stats.awayTacklesWon ?? 0,

      homeInterceptions:
        stats.homeInterceptions ?? 0,

      awayInterceptions:
        stats.awayInterceptions ?? 0,

      homeSaves:
        stats.homeSaves ?? 0,

      awaySaves:
        stats.awaySaves ?? 0,

      homeFoulsCommitted:
        stats.homeFoulsCommitted ?? 0,

      awayFoulsCommitted:
        stats.awayFoulsCommitted ?? 0,

      homeOffsides:
        stats.homeOffsides ?? 0,

      awayOffsides:
        stats.awayOffsides ?? 0,

      homeCorners:
        stats.homeCorners ?? 0,

      awayCorners:
        stats.awayCorners ?? 0,

      homeFreeKicks:
        stats.homeFreeKicks ?? 0,

      awayFreeKicks:
        stats.awayFreeKicks ?? 0,

      homePenaltyKicks:
        stats.homePenaltyKicks ?? 0,

      awayPenaltyKicks:
        stats.awayPenaltyKicks ?? 0,

      homeYellowCards:
        stats.homeYellowCards ?? 0,

      awayYellowCards:
        stats.awayYellowCards ?? 0
    });

    this.homePlayers.clear();
    this.awayPlayers.clear();

    for (
      const player of
      stats.homePlayers ?? []
    ) {
      this.homePlayers.push(
        this.createPlayerForm(
          player,
          this.homeTeamPlayers
        )
      );
    }

    for (
      const player of
      stats.awayPlayers ?? []
    ) {
      this.awayPlayers.push(
        this.createPlayerForm(
          player,
          this.awayTeamPlayers
        )
      );
    }

    this.ensurePlayerRows();
    this.cdr.markForCheck();
  }

  private setInitialValues(): void {
    this.form.patchValue({
      homeGoals: this.homeGoals,
      awayGoals: this.awayGoals
    });

    this.homePlayers.clear();
    this.awayPlayers.clear();

    this.ensurePlayerRows();
    this.cdr.markForCheck();
  }

  private populateSummaryForm(): void {
    const preview =
      this.summaryOcrPreview;

    if (!preview) {
      this.form.patchValue({
        homeGoals: this.homeGoals,
        awayGoals: this.awayGoals
      });

      this.cdr.markForCheck();
      return;
    }

    this.form.patchValue({
      homeGoals:
        preview.homeGoals,

      awayGoals:
        preview.awayGoals,

      homePossession:
        preview.homePossession,

      awayPossession:
        preview.awayPossession,

      homeShots:
        preview.homeShots,

      awayShots:
        preview.awayShots,

      homeExpectedGoals:
        preview.homeExpectedGoals,

      awayExpectedGoals:
        preview.awayExpectedGoals,

      homePasses:
        preview.homePasses,

      awayPasses:
        preview.awayPasses,

      homeTackles:
        preview.homeTackles,

      awayTackles:
        preview.awayTackles,

      homeTacklesWon:
        preview.homeTacklesWon,

      awayTacklesWon:
        preview.awayTacklesWon,

      homeInterceptions:
        preview.homeInterceptions,

      awayInterceptions:
        preview.awayInterceptions,

      homeSaves:
        preview.homeSaves,

      awaySaves:
        preview.awaySaves,

      homeFoulsCommitted:
        preview.homeFoulsCommitted,

      awayFoulsCommitted:
        preview.awayFoulsCommitted,

      homeOffsides:
        preview.homeOffsides,

      awayOffsides:
        preview.awayOffsides,

      homeCorners:
        preview.homeCorners,

      awayCorners:
        preview.awayCorners,

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
      this.homePerformanceOcrPreview
        ?.players ?? [];

    const awayPlayers =
      this.awayPerformanceOcrPreview
        ?.players ?? [];

    for (const player of homePlayers) {
      this.homePlayers.push(
        this.createPlayerForm(
          player,
          this.homeTeamPlayers
        )
      );
    }

    for (const player of awayPlayers) {
      this.awayPlayers.push(
        this.createPlayerForm(
          player,
          this.awayTeamPlayers
        )
      );
    }

    this.ensurePlayerRows();
    this.cdr.markForCheck();
  }

  private ensurePlayerRows(): void {
    if (this.homePlayers.length === 0) {
      this.homePlayers.push(
        this.createPlayerForm(
          undefined,
          this.homeTeamPlayers
        )
      );
    }

    if (this.awayPlayers.length === 0) {
      this.awayPlayers.push(
        this.createPlayerForm(
          undefined,
          this.awayTeamPlayers
        )
      );
    }
  }

  private createPlayerForm(
    player?:
      | ManualPlayer
      | MatchPlayerStatsOcrPreview,

    teamPlayers:
      TeamPlayerOption[] = []
  ): FormGroup {
    const playerName =
      player?.playerName ?? '';

    const matchingPlayer =
      this.findPlayerByName(
        teamPlayers,
        playerName
      );

    return this.fb.group({
      gamePlayerId: [
        matchingPlayer?.gamePlayerId ?? ''
      ],

      position: [
        player?.position ??
        matchingPlayer?.position ??
        '',
        Validators.required
      ],

      playerName: [
        playerName ||
        matchingPlayer?.name ||
        '',
        Validators.required
      ],

      rating: [
        player?.rating ?? null,
        [
          Validators.required,
          Validators.min(0),
          Validators.max(10)
        ]
      ],

      goals: [
        player?.goals ?? null,
        [
          Validators.required,
          Validators.min(0)
        ]
      ],

      assists: [
        player?.assists ?? null,
        [
          Validators.required,
          Validators.min(0)
        ]
      ]
    });
  }

  private syncPlayersWithRosters(): void {
    this.syncPlayerArrayWithRoster(
      this.homePlayers,
      this.homeTeamPlayers
    );

    this.syncPlayerArrayWithRoster(
      this.awayPlayers,
      this.awayTeamPlayers
    );

    this.cdr.markForCheck();
  }

  private syncPlayerArrayWithRoster(
    players: FormArray<FormGroup>,
    teamPlayers: TeamPlayerOption[]
  ): void {
    for (
      const playerForm of
      players.controls
    ) {
      const currentId = String(
        playerForm
          .get('gamePlayerId')
          ?.value ?? ''
      );

      if (currentId) {
        continue;
      }

      const playerName = String(
        playerForm
          .get('playerName')
          ?.value ?? ''
      );

      if (!playerName) {
        continue;
      }

      const matchingPlayer =
        this.findPlayerByName(
          teamPlayers,
          playerName
        );

      if (!matchingPlayer) {
        continue;
      }

      playerForm.patchValue(
        {
          gamePlayerId:
            matchingPlayer.gamePlayerId,

          playerName:
            matchingPlayer.name,

          position:
            playerForm
              .get('position')
              ?.value ||
            matchingPlayer.position
        },
        {
          emitEvent: false
        }
      );
    }
  }

  private findPlayerByName(
    teamPlayers: TeamPlayerOption[],
    playerName: string
  ): TeamPlayerOption | undefined {
    const normalizedPlayerName =
      this.normalizePlayerName(playerName);

    if (!normalizedPlayerName) {
      return undefined;
    }

    return teamPlayers.find(player =>
      this.normalizePlayerName(
        player.name
      ) === normalizedPlayerName
    );
  }

  private normalizePlayerName(
    value: string
  ): string {
    return String(value ?? '')
      .normalize('NFD')
      .replace(
        /[\u0300-\u036f]/g,
        ''
      )
      .trim()
      .toLowerCase()
      .replace(/\s+/g, ' ');
  }

  private toRequestPlayers(
    players: FormArray<FormGroup>
  ): ManualPlayer[] {
    return players.controls.map(control => {
      const player = control.getRawValue();

      return {
        gamePlayerId:
          player.gamePlayerId || null,

        position: String(
          player.position ?? ''
        ).trim(),

        playerName: String(
          player.playerName ?? ''
        ).trim(),

        rating: Number(
          player.rating ?? 0
        ),

        goals: Number(
          player.goals ?? 0
        ),

        assists: Number(
          player.assists ?? 0
        )
      } as ManualPlayer;
    });
  }

  private removeEmptyPlayerRows(
    players: FormArray<FormGroup>
  ): void {
    for (
      let index = players.length - 1;
      index >= 0;
      index--
    ) {
      const player =
        players.at(index).getRawValue();

      const gamePlayerId =
        String(
          player.gamePlayerId ?? ''
        ).trim();

      const position =
        String(
          player.position ?? ''
        ).trim();

      const playerName =
        String(
          player.playerName ?? ''
        ).trim();

      const rating =
        Number(player.rating ?? 0);

      const goals =
        Number(player.goals ?? 0);

      const assists =
        Number(player.assists ?? 0);

      const isCompletelyEmpty =
        !gamePlayerId &&
        !position &&
        !playerName &&
        rating === 0 &&
        goals === 0 &&
        assists === 0;

      if (isCompletelyEmpty) {
        players.removeAt(index);
      }
    }
  }
}
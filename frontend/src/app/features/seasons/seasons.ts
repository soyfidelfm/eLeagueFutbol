import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize, Observable } from 'rxjs';

import {
  Season,
  CreateSeasonDto,
  UpdateSeasonDto,
  SeasonPhase
} from '../../core/models/season.model';

import { SeasonService } from '../../core/services/season.service';
import { MatchService } from '../../core/services/match.service';
import { UserService } from '../../core/services/user.service';
import { AppLoading } from '../../shared/components/app-loading/app-loading';
import { ConfirmDialogService } from '../../shared/components/confirm-dialog/confirm-dialog.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LocalizedDatePipe } from '../../core/i18n/localized-date.pipe';
import { EmptyState } from '../../shared/components/empty-state/empty-state';

@Component({
  selector: 'app-season',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    AppLoading,
    TranslatePipe, LocalizedDatePipe, EmptyState
  ],
  templateUrl: './seasons.html',
  styleUrl: './seasons.scss'
})
export class Seasons implements OnInit {
  seasons: Season[] = [];

  view: 'current' | 'list' | 'create' = 'list';

  isLoading = false;
  isSaving = false;
  isEditMode = false;
  isAdmin = false;

  selectedSeasonId: string | null = null;
  errorMessage = '';

  form = {
    name: '',
    description: '',
    startDate: '',
    endDate: '',
    isCurrent: false,
    isActive: true,
    registrationOpen: true,
    maxPlayers: 20,
    totalWeeks: 8
  };

  constructor(
    private seasonService: SeasonService,
    private matchService: MatchService,
    private userService: UserService,
    private route: ActivatedRoute,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private i18n: TranslationService,
    private confirmDialog: ConfirmDialogService
  ) {}

  ngOnInit(): void {
    this.route.queryParamMap.subscribe(params => {
      const viewParam = params.get('view');

      if (viewParam === 'create') {
        this.view = 'create';
      } else if (viewParam === 'current') {
        this.view = 'current';
      } else {
        this.view = 'list';
      }

      this.enforceAdminView();
    });

    this.loadCurrentUser();
    this.loadSeasons();
  }

  get currentSeason(): Season | null {
    return this.seasons.find(season => season.isCurrent) ?? null;
  }

  loadCurrentUser(): void {
    this.userService.getMe().subscribe({
      next: () => {
        this.isAdmin = this.userService.isAdmin;
        this.enforceAdminView();
        this.cdr.detectChanges();
      },
      error: () => {
        this.isAdmin = false;
        this.enforceAdminView();
        this.cdr.detectChanges();
      }
    });
  }

  loadSeasons(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.seasonService.getAll()
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: data => {
          this.seasons = Array.isArray(data) ? data : [];
        },
        error: err => {
          console.error('Error loading seasons:', err);
          this.seasons = [];
          this.errorMessage = 'Could not load seasons.';
        }
      });
  }

  phaseLabel(season: Season | null): string {
    const phase = (season?.phase ?? this.inferPhase(season)) as SeasonPhase;

    switch (phase) {
      case 'DraftReady':
        return 'Draft ready';
      case 'Drafting':
        return 'Drafting';
      case 'Competition':
        return 'Competition';
      case 'Closed':
        return 'Closed';
      default:
        return season?.registrationOpen ? 'Registration' : 'Registration closed';
    }
  }

  phaseClass(season: Season | null): string {
    const phase = season?.phase ?? this.inferPhase(season);

    if (phase === 'Closed') return 'off';
    if (phase === 'Competition') return 'active';
    if (phase === 'Drafting' || phase === 'DraftReady') return 'current';
    return season?.registrationOpen ? 'active' : 'off';
  }

  openCreate(): void {
    if (!this.isAdmin) {
      return;
    }

    this.resetForm();
    this.view = 'create';
  }

  save(): void {
    if (!this.isAdmin) {
      alert(this.i18n.t('Only admins can manage seasons.'));
      return;
    }

    if (!this.form.name.trim()) {
      alert(this.i18n.t('Season name is required'));
      return;
    }

    if (!this.form.startDate) {
      alert(this.i18n.t('Start date is required'));
      return;
    }

    if (!this.form.endDate) {
      alert(this.i18n.t('End date is required'));
      return;
    }

    if (new Date(this.form.endDate) <= new Date(this.form.startDate)) {
      alert(this.i18n.t('End date must be greater than start date'));
      return;
    }

    if (!this.form.totalWeeks || Number(this.form.totalWeeks) < 1) {
      alert(this.i18n.t('Total weeks must be at least 1'));
      return;
    }

    this.isSaving = true;

    const dto = this.buildDto();

    let request$: Observable<Season | void>;

    if (this.isEditMode && this.selectedSeasonId) {
      request$ = this.seasonService.update(
        this.selectedSeasonId,
        dto as UpdateSeasonDto
      );
    } else {
      request$ = this.seasonService.create(dto);
    }

    request$
      .pipe(
        finalize(() => {
          this.isSaving = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: () => {
          this.resetForm();
          this.loadSeasons();

          this.router.navigate(['/seasons'], {
            queryParams: {
              view: 'list'
            }
          });
        },
        error: err => {
          console.error('Error saving season:', err);
          alert(err?.error?.message || this.i18n.t('Error saving season'));
        }
      });
  }

  edit(season: Season | null): void {
    if (!this.isAdmin || !season) return;

    this.isEditMode = true;
    this.selectedSeasonId = season.seasonId;

    this.form = {
      name: season.name ?? '',
      description: season.description ?? '',
      startDate: this.toDateTimeLocal(season.startDate),
      endDate: this.toDateTimeLocal(season.endDate),
      isCurrent: !!season.isCurrent,
      isActive: !!season.isActive,
      registrationOpen: !!season.registrationOpen,
      maxPlayers: season.maxPlayers ?? 20,
      totalWeeks: season.totalWeeks && season.totalWeeks > 0
        ? season.totalWeeks
        : 8
    };

    this.router.navigate(['/seasons'], {
      queryParams: {
        view: 'create'
      }
    });

    window.scrollTo({
      top: 0,
      behavior: 'smooth'
    });
  }

  async generateSchedule(season: Season): Promise<void> {
    if (!this.isAdmin || !season?.seasonId) {
      return;
    }

    const ok = await this.confirmDialog.confirm({
      title: 'Generate schedule for {name}?',
      description: 'Matches will be created for every week of this season.',
      params: { name: season.name },
      confirmLabel: 'Generate',
      danger: false
    });

    if (!ok) {
      return;
    }

    this.runAction(
      this.matchService.generateSchedule(season.seasonId),
      'Schedule generated successfully.'
    );
  }

  async closeRegistration(season: Season): Promise<void> {
    if (!(await this.confirmAdmin(season, {
      title: 'Close registration for {name}?',
      description: 'Players will no longer be able to join this season.',
      confirmLabel: 'Close registration',
      danger: true
    }))) {
      return;
    }

    this.runAction(
      this.seasonService.closeRegistration(season.seasonId),
      'Registration closed.'
    );
  }

  async openRegistration(season: Season): Promise<void> {
    if (!(await this.confirmAdmin(season, {
      title: 'Open registration for {name}?',
      description: 'Players will be able to join this season.',
      confirmLabel: 'Open registration',
      danger: false
    }))) {
      return;
    }

    this.runAction(
      this.seasonService.openRegistration(season.seasonId),
      'Registration opened.'
    );
  }

  async setCurrent(season: Season): Promise<void> {
    if (!(await this.confirmAdmin(season, {
      title: 'Set {name} as the current season?',
      description: 'Only one season can be current. Others will be unmarked.',
      confirmLabel: 'Make current',
      danger: false
    }))) {
      return;
    }

    this.runAction(
      this.seasonService.setCurrent(season.seasonId),
      'Current season updated.'
    );
  }

  async closeSeason(season: Season): Promise<void> {
    if (!(await this.confirmAdmin(season, {
      title: 'Close {name}?',
      description: 'Pending transfer offers will be cancelled and results will be locked. This cannot be undone.',
      confirmLabel: 'Close season',
      danger: true
    }))) {
      return;
    }

    this.runAction(
      this.seasonService.closeSeason(season.seasonId),
      'Season closed.'
    );
  }

  async activateSeason(season: Season): Promise<void> {
    if (!(await this.confirmAdmin(season, {
      title: 'Activate {name}?',
      description: 'The season will be visible across the platform.',
      confirmLabel: 'Activate',
      danger: false
    }))) {
      return;
    }

    this.runAction(
      this.seasonService.activateSeason(season.seasonId),
      'Season activated.'
    );
  }

  async delete(seasonId: string): Promise<void> {
    if (!this.isAdmin) {
      return;
    }

    const ok = await this.confirmDialog.confirm({
      title: 'Delete this season?',
      description: 'The season and its configuration will be removed. This cannot be undone.',
      confirmLabel: 'Delete'
    });

    if (!ok) return;

    this.seasonService.remove(seasonId).subscribe({
      next: () => this.loadSeasons(),
      error: err => {
        console.error('Error deleting season:', err);
        alert(err?.error?.message || this.i18n.t('Error deleting season'));
      }
    });
  }

  resetForm(): void {
    this.isEditMode = false;
    this.selectedSeasonId = null;

    this.form = {
      name: '',
      description: '',
      startDate: '',
      endDate: '',
      isCurrent: false,
      isActive: true,
      registrationOpen: true,
      maxPlayers: 20,
      totalWeeks: 8
    };
  }

  private confirmAdmin(
    season: Season | null,
    options: { title: string; description: string; confirmLabel: string; danger: boolean }
  ): Promise<boolean> {
    if (!this.isAdmin || !season?.seasonId) {
      return Promise.resolve(false);
    }

    return this.confirmDialog.confirm({ ...options, params: { name: season.name } });
  }

  private runAction(request$: Observable<unknown>, successMessage: string): void {
    this.isSaving = true;

    request$
      .pipe(
        finalize(() => {
          this.isSaving = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: () => {
          this.loadSeasons();
          alert(this.i18n.t(successMessage));
        },
        error: err => {
          console.error(err);
          alert(err?.error?.message || this.i18n.t('Action failed.'));
        }
      });
  }

  private inferPhase(season: Season | null): SeasonPhase {
    if (!season) return 'Registration';
    if (!season.isActive) return 'Closed';
    if (season.draftActive) return 'Drafting';
    if (season.hasSchedule) return 'Competition';
    if (season.hasDraft) return 'DraftReady';
    return 'Registration';
  }

  private enforceAdminView(): void {
    if (!this.isAdmin && this.view === 'create') {
      this.view = 'list';
      this.resetForm();
    }
  }

  private buildDto(): CreateSeasonDto {
    return {
      name: this.form.name.trim(),
      description: this.form.description.trim(),
      startDate: this.form.startDate,
      endDate: this.form.endDate,
      timeZone: this.getClientTimeZone(),
      isCurrent: this.form.isCurrent,
      isActive: this.form.isActive,
      registrationOpen: this.form.registrationOpen,
      maxPlayers: Number(this.form.maxPlayers),
      totalWeeks: Number(this.form.totalWeeks)
    };
  }

  private getClientTimeZone(): string {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';
  }

  private toDateTimeLocal(value: string | Date | null | undefined): string {
    if (!value) return '';

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) return '';

    const year = date.getFullYear();
    const month = this.pad(date.getMonth() + 1);
    const day = this.pad(date.getDate());
    const hours = this.pad(date.getHours());
    const minutes = this.pad(date.getMinutes());

    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }

  private pad(value: number): string {
    return value.toString().padStart(2, '0');
  }
}

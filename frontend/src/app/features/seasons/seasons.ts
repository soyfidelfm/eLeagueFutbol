import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize, Observable } from 'rxjs';

import {
  Season,
  CreateSeasonDto,
  UpdateSeasonDto
} from '../../core/models/season.model';

import { SeasonService } from '../../core/services/season.service';
import { MatchService } from '../../core/services/match.service';
import { UserService } from '../../core/services/user.service';
import { AppLoading } from '../../shared/components/app-loading/app-loading';

@Component({
  selector: 'app-season',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    AppLoading
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
    private cdr: ChangeDetectorRef
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

  openCreate(): void {
    if (!this.isAdmin) {
      return;
    }

    this.resetForm();
    this.view = 'create';
  }

  save(): void {
    if (!this.isAdmin) {
      alert('Only admins can manage seasons.');
      return;
    }

    if (!this.form.name.trim()) {
      alert('Season name is required');
      return;
    }

    if (!this.form.startDate) {
      alert('Start date is required');
      return;
    }

    if (!this.form.endDate) {
      alert('End date is required');
      return;
    }

    if (new Date(this.form.endDate) <= new Date(this.form.startDate)) {
      alert('End date must be greater than start date');
      return;
    }

    if (!this.form.totalWeeks || Number(this.form.totalWeeks) < 1) {
      alert('Total weeks must be at least 1');
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
          alert(err?.error?.message || 'Error saving season');
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

  generateSchedule(season: Season): void {
    if (!this.isAdmin || !season?.seasonId) {
      return;
    }

    if (!confirm(`Generate schedule for ${season.name}?`)) {
      return;
    }

    this.isSaving = true;

    this.matchService.generateSchedule(season.seasonId)
      .pipe(
        finalize(() => {
          this.isSaving = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: () => {
          this.loadSeasons();
          alert('Schedule generated successfully.');
        },
        error: err => {
          console.error('Error generating schedule:', err);
          alert(err?.error?.message || 'Error generating schedule.');
        }
      });
  }

  delete(seasonId: string): void {
    if (!this.isAdmin) {
      return;
    }

    if (!confirm('Are you sure you want to delete this season?')) return;

    this.seasonService.remove(seasonId).subscribe({
      next: () => this.loadSeasons(),
      error: err => {
        console.error('Error deleting season:', err);
        alert(err?.error?.message || 'Error deleting season');
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

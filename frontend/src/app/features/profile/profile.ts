import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  EventEmitter,
  OnInit,
  Output,
  ViewChild
} from '@angular/core';
import { FormsModule } from '@angular/forms';

import {
  MatAutocompleteModule,
  MatAutocompleteTrigger
} from '@angular/material/autocomplete';

import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

import { finalize } from 'rxjs';

import { User } from '../../core/models/user.model';
import { UserService } from '../../core/services/user.service';
import { GameClub } from '../../core/models/game-club.model';
import { GameClubService } from '../../core/services/game-club.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatAutocompleteModule,
    MatFormFieldModule,
    MatInputModule
  ],
  templateUrl: './profile.html',
  styleUrl: './profile.scss'
})
export class Profile implements OnInit {
  @Output() saved = new EventEmitter<void>();

  @ViewChild('leagueTrigger')
  leagueTrigger?: MatAutocompleteTrigger;

  @ViewChild('teamTrigger')
  teamTrigger?: MatAutocompleteTrigger;

  leagues: string[] = [];
  filteredLeagues: string[] = [];
  leagueSearchText = '';

  gameClubs: GameClub[] = [];
  filteredGameClubs: GameClub[] = [];
  teamSearchText = '';

  selectedLeague = '';
  selectedGameClubId = '';

  user: User | null = null;

  isLoading = false;
  isSaving = false;

  errorMessage = '';
  successMessage = '';

  form = {
    displayName: '',
    avatarUrl: '',
    timeZone: '',
    clubName: '',
    gameClubId: '',
    gamerTag: ''
  };

  constructor(
    private userService: UserService,
    private cdr: ChangeDetectorRef,
    private gameClubService: GameClubService
  ) {}

  ngOnInit(): void {
    this.form.timeZone = this.getCurrentTimeZone();

    this.loadLeagues();
    this.loadUser();
  }

  private getCurrentTimeZone(): string {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';
  }

  private loadLeagues(): void {
    this.gameClubService.getLeagues().subscribe({
      next: leagues => {
        this.leagues = leagues;
        this.filteredLeagues = leagues;
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Error loading leagues:', error);
      }
    });
  }

  loadUser(): void {
    const userId = localStorage.getItem('userId');

    this.errorMessage = '';
    this.successMessage = '';
    this.user = null;
    this.isLoading = true;

    if (!userId) {
      this.isLoading = false;
      this.errorMessage = 'User not found. Please login again.';
      this.cdr.detectChanges();
      return;
    }

    this.userService.getById(userId)
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: user => {
          this.user = user;

          const currentTimeZone = this.getCurrentTimeZone();

          this.form = {
            displayName: user.displayName ?? '',
            avatarUrl: user.avatarUrl ?? '',
            timeZone: currentTimeZone,
            clubName: user.clubName ?? '',
            gameClubId: user.gameClubId ?? '',
            gamerTag: user.gamerTag ?? ''
          };

          this.selectedLeague = user.gameClubLeague ?? '';
          this.leagueSearchText = user.gameClubLeague ?? '';

          this.selectedGameClubId = user.gameClubId ?? '';
          this.form.gameClubId = this.selectedGameClubId;

          if (this.selectedLeague) {
            this.loadClubsByLeague(this.selectedLeague, true);
          }

          this.cdr.detectChanges();
        },
        error: error => {
          console.error('Error loading profile:', error);
          this.errorMessage = 'Error loading profile.';
          this.cdr.detectChanges();
        }
      });
  }

  filterLeagues(): void {
    const value = this.leagueSearchText.toLowerCase().trim();

    this.filteredLeagues = this.leagues.filter(league =>
      league.toLowerCase().includes(value)
    );

    if (this.selectedLeague !== this.leagueSearchText) {
      this.selectedLeague = '';
      this.selectedGameClubId = '';
      this.form.gameClubId = '';
      this.teamSearchText = '';
      this.gameClubs = [];
      this.filteredGameClubs = [];
    }
  }

  selectLeague(league: string): void {
    this.selectedLeague = league;
    this.leagueSearchText = league;

    this.selectedGameClubId = '';
    this.form.gameClubId = '';
    this.teamSearchText = '';
    this.gameClubs = [];
    this.filteredGameClubs = [];
    this.filteredLeagues = [];

    this.closeLeaguePanel();

    this.loadClubsByLeague(league, false);
  }

  private closeLeaguePanel(): void {
    this.leagueTrigger?.closePanel();

    setTimeout(() => {
      this.leagueTrigger?.closePanel();

      const activeElement = document.activeElement as HTMLElement | null;
      activeElement?.blur();
    }, 0);
  }

  private loadClubsByLeague(
    league: string,
    shouldHydrateSelectedTeam: boolean
  ): void {
    this.gameClubService.getByLeague(league).subscribe({
      next: clubs => {
        this.gameClubs = clubs;
        this.filteredGameClubs = clubs;

        if (shouldHydrateSelectedTeam && this.selectedGameClubId) {
          const selectedClub = clubs.find(
            club => club.gameClubId === this.selectedGameClubId
          );

          this.teamSearchText = selectedClub?.name ?? '';
        }

        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Error loading clubs:', error);
      }
    });
  }

  filterTeams(): void {
    const value = this.teamSearchText.toLowerCase().trim();

    this.filteredGameClubs = this.gameClubs.filter(club =>
      club.name.toLowerCase().includes(value)
    );

    const selectedClub = this.gameClubs.find(
      club => club.gameClubId === this.selectedGameClubId
    );

    if (selectedClub?.name !== this.teamSearchText) {
      this.selectedGameClubId = '';
      this.form.gameClubId = '';
    }
  }

  selectTeam(club: GameClub): void {
    this.selectedGameClubId = club.gameClubId;
    this.form.gameClubId = club.gameClubId;
    this.teamSearchText = club.name;
    this.filteredGameClubs = [];

    this.closeTeamPanel();
  }

  private closeTeamPanel(): void {
    this.teamTrigger?.closePanel();

    setTimeout(() => {
      this.teamTrigger?.closePanel();

      const activeElement = document.activeElement as HTMLElement | null;
      activeElement?.blur();
    }, 0);
  }

  save(): void {
    if (!this.user) {
      this.errorMessage = 'User not loaded.';
      this.cdr.detectChanges();
      return;
    }

    this.form.timeZone = this.getCurrentTimeZone();
    this.form.gameClubId = this.selectedGameClubId;

    this.isSaving = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.userService.update(this.user.userId, this.form)
      .pipe(
        finalize(() => {
          this.isSaving = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: user => {
          this.user = user;

          const currentTimeZone = this.getCurrentTimeZone();

          this.form = {
            displayName: user.displayName ?? '',
            avatarUrl: user.avatarUrl ?? '',
            timeZone: currentTimeZone,
            clubName: user.clubName ?? '',
            gameClubId: user.gameClubId ?? '',
            gamerTag: user.gamerTag ?? ''
          };

          this.selectedLeague = user.gameClubLeague ?? '';
          this.leagueSearchText = user.gameClubLeague ?? '';

          this.selectedGameClubId = user.gameClubId ?? '';
          this.form.gameClubId = this.selectedGameClubId;

          if (this.selectedLeague) {
            this.loadClubsByLeague(this.selectedLeague, true);
          }

          this.successMessage = 'Profile updated successfully.';
          this.saved.emit();
        },
        error: error => {
          console.error('Error updating profile:', error);
          this.errorMessage = 'Error updating profile.';
          this.cdr.detectChanges();
        }
      });
  }
}
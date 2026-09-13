import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';

import { AppLoading } from '../../shared/components/app-loading/app-loading';
import { TeamApiService, TeamDto } from '../../core/services/team.service';
import { UserService, UserSearchResult } from '../../core/services/user.service';

@Component({
  selector: 'app-teams',
  standalone: true,
  imports: [CommonModule, FormsModule, AppLoading],
  templateUrl: './teams.html',
  styleUrl: './teams.scss'
})
export class Teams implements OnInit {
  isLoading = true;
  errorMessage = '';
  teamName = '';
  teamDescription = '';
  searchTerm = '';
  searchResults: UserSearchResult[] = [];
  teams: TeamDto[] = [];
  mine: TeamDto | null = null;

  constructor(
    private teamApi: TeamApiService,
    private users: UserService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.isLoading = true;
    this.teamApi.getAll()
      .pipe(finalize(() => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: teams => {
          this.teams = teams ?? [];
          this.mine = this.teams.find(x => x.isMine) ?? null;
          this.errorMessage = '';
        },
        error: err => {
          this.errorMessage = err?.error?.message || 'Could not load teams.';
        }
      });
  }

  createTeam(): void {
    if (!this.teamName.trim()) {
      this.errorMessage = 'Team name is required.';
      return;
    }
    this.teamApi.create(this.teamName.trim(), this.teamDescription.trim()).subscribe({
      next: () => {
        this.teamName = '';
        this.teamDescription = '';
        this.reload();
      },
      error: err => {
        this.errorMessage = err?.error?.message || 'Could not create team.';
        this.cdr.detectChanges();
      }
    });
  }

  search(): void {
    if (!this.searchTerm.trim()) {
      this.searchResults = [];
      return;
    }
    this.users.searchUsers(this.searchTerm.trim()).subscribe({
      next: results => {
        this.searchResults = results ?? [];
        this.cdr.detectChanges();
      }
    });
  }

  addMember(userId: string): void {
    if (!this.mine) {
      return;
    }
    this.teamApi.addMember(this.mine.teamId, userId).subscribe({
      next: () => {
        this.searchTerm = '';
        this.searchResults = [];
        this.reload();
      },
      error: err => {
        this.errorMessage = err?.error?.message || 'Could not add member.';
        this.cdr.detectChanges();
      }
    });
  }

  removeMember(userId: string): void {
    if (!this.mine) {
      return;
    }
    this.teamApi.removeMember(this.mine.teamId, userId).subscribe({
      next: () => this.reload(),
      error: err => {
        this.errorMessage = err?.error?.message || 'Could not remove member.';
        this.cdr.detectChanges();
      }
    });
  }
}

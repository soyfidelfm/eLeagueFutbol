import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AppLoading } from '../../shared/components/app-loading/app-loading';
import { ClubService } from '../../core/services/club.service';
import { MyClubDto, MyClubPlayerDto } from '../../core/models/my-club.model';

@Component({
  selector: 'app-teams',
  standalone: true,
  imports: [CommonModule, RouterLink, AppLoading],
  templateUrl: './teams.html',
  styleUrl: './teams.scss'
})
export class Teams implements OnInit {
  isLoading = true;
  errorMessage = '';
  club: MyClubDto | null = null;

  constructor(
    private clubs: ClubService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.reload();
  }

  get players(): MyClubPlayerDto[] {
    return this.club?.players ?? [];
  }

  get starters(): MyClubPlayerDto[] {
    return this.club?.startingPlayers ?? [];
  }

  reload(): void {
    this.isLoading = true;
    this.clubs.getMyClub()
      .pipe(finalize(() => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: club => {
          this.club = club;
          this.errorMessage = '';
        },
        error: err => {
          this.club = null;
          this.errorMessage = err?.error?.message || 'Todavia no tienes plantilla. Entra al draft para elegir futbolistas.';
        }
      });
  }
}

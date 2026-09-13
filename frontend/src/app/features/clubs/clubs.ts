import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize, switchMap } from 'rxjs';

import { ClubService } from '../../core/services/club.service';
import { CloudinaryService } from '../../core/services/cloudinary.service';
import { MyClubDto } from '../../core/models/my-club.model';
import { SquadLineupModalComponent } from '../clubs/components/squad-lineup-modal/squad-lineup-modal';
import { AppLoading } from '../../shared/components/app-loading/app-loading';

import {
  FORMATIONS,
  FORMATION_LAYOUTS,
  FormationPlayerPosition
} from '../../core/constants/formations';

type ClubPlayer = NonNullable<MyClubDto['players']>[number];

@Component({
  selector: 'app-clubs',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    SquadLineupModalComponent,
    AppLoading
  ],
  templateUrl: './clubs.html',
  styleUrl: './clubs.scss'
})
export class Clubs implements OnInit {

  club: MyClubDto | null = null;

  formations = FORMATIONS;
  formationLayouts = FORMATION_LAYOUTS;

  formationIndex = 0;
  selectedFormationLayout: FormationPlayerPosition[] = [];

  chemistry = 0;
  startingPlayers: ClubPlayer[] = [];

  isSquadModalOpen = false;

  isLoading = false;
  isUploadingLogo = false;
  errorMessage = '';

  constructor(
    private clubService: ClubService,
    private cloudinaryService: CloudinaryService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.loadMyClub();
  }

  get selectedFormation(): string {
    return this.formations[this.formationIndex];
  }

  get players(): ClubPlayer[] {
    return this.club?.players ?? [];
  }

  get activities() {
    return this.club?.activities ?? [];
  }

  get stats() {
    if (!this.club) {
      return [];
    }

    return [
      {
        label: 'Matches Played',
        value: this.club.matchesPlayed.toString(),
        sub: 'Match History',
        icon: 'fa-solid fa-shield-halved'
      },
      {
        label: 'Wins',
        value: this.club.wins.toString(),
        sub: this.getWinPercentage(),
        icon: 'fa-solid fa-trophy'
      },
      {
        label: 'Goals Scored',
        value: this.club.goalsFor.toString(),
        sub: `${this.getAverage(this.club.goalsFor)} per match`,
        icon: 'fa-solid fa-crosshairs'
      },
      {
        label: 'Goals Conceded',
        value: this.club.goalsAgainst.toString(),
        sub: `${this.getAverage(this.club.goalsAgainst)} per match`,
        icon: 'fa-solid fa-shield'
      },
      {
        label: 'Club Rating',
        value: this.club.rating.toString(),
        sub: 'Current Rating',
        icon: 'fa-regular fa-star'
      }
    ];
  }

  isStarter(playerId: string): boolean {
    return this.startingPlayers.some(x => x.playerId === playerId);
  }

  loadMyClub(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.club = null;
    this.startingPlayers = [];

    this.cdr.detectChanges();

    this.clubService.getMyClub()
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (response: MyClubDto) => {
          this.club = response;
          this.applyClubLineup(response);
          this.cdr.detectChanges();
        },
        error: err => {
          console.error('Error loading club', err);
          this.errorMessage = 'Unable to load club information.';
          this.cdr.detectChanges();
        }
      });
  }

  openSquadModal(): void {
    this.isSquadModalOpen = true;
    this.cdr.detectChanges();
  }

  closeSquadModal(): void {
    this.isSquadModalOpen = false;
    this.cdr.detectChanges();
  }

  saveLineup(payload: {
    formation: string;
    players: {
      playerId: string;
      slotIndex: number;
      position: string;
    }[];
  }): void {
    if (payload.players.length !== 11) {
      this.errorMessage = 'Starting XI must have 11 players.';
      this.cdr.detectChanges();
      return;
    }

    this.clubService.updateMyClubLineup(payload)
      .pipe(
        finalize(() => {
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (updatedClub: MyClubDto) => {
          this.club = updatedClub;
          this.applyClubLineup(updatedClub, payload.formation);
          this.closeSquadModal();
        },
        error: err => {
          console.error('Error saving lineup', err);
          this.errorMessage = 'Unable to save lineup.';
          this.cdr.detectChanges();
        }
      });
  }

  onLogoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) {
      return;
    }

    if (!this.club) {
      this.errorMessage = 'Club information is not loaded yet.';
      input.value = '';
      this.cdr.detectChanges();
      return;
    }

    const allowedTypes = ['image/png', 'image/jpeg', 'image/webp'];

    if (!allowedTypes.includes(file.type)) {
      this.errorMessage = 'Only PNG, JPG or WEBP images are allowed.';
      input.value = '';
      this.cdr.detectChanges();
      return;
    }

    this.isUploadingLogo = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.cloudinaryService.uploadImage(file)
      .pipe(
        switchMap(uploadResult =>
          this.clubService.updateMyClubLogo(uploadResult.secureUrl)
        ),
        finalize(() => {
          this.isUploadingLogo = false;
          input.value = '';
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (updatedClub: MyClubDto) => {
          this.club = updatedClub;
          this.applyClubLineup(updatedClub);
          this.cdr.detectChanges();
        },
        error: err => {
          console.error('Error uploading club logo', err);
          this.errorMessage = 'Unable to upload club logo.';
          this.cdr.detectChanges();
        }
      });
  }

  onImageError(event: Event): void {
    const img = event.target as HTMLImageElement;

    if (!img.src.includes('/images/logo-clear.png')) {
      img.src = '/images/logo-clear.png';
    }
  }

  onPlayerImageError(event: Event): void {
    const img = event.target as HTMLImageElement;

    if (!img.src.includes('/images/avatar.png')) {
      img.src = '/images/avatar.png';
    }
  }

  private applyClubLineup(club: MyClubDto, fallbackFormation?: string): void {
    const savedFormation = club.formation || fallbackFormation || '4-3-3 Control';
    const savedFormationIndex = this.formations.findIndex(x => x === savedFormation);

    this.formationIndex = savedFormationIndex >= 0 ? savedFormationIndex : 0;
    this.selectedFormationLayout = this.getFormationLayout(this.selectedFormation);

    this.setStartingPlayers(club.startingPlayers ?? []);

    this.chemistry = club.chemistry ?? 0;
  }

  private setStartingPlayers(players: ClubPlayer[]): void {
    this.startingPlayers = [...players]
      .sort((a, b) => a.slotIndex - b.slotIndex);
  }

  private getFormationLayout(formation: string): FormationPlayerPosition[] {
    return [
      ...(this.formationLayouts[formation] ?? [])
    ];
  }

  private getWinPercentage(): string {
    if (!this.club || this.club.matchesPlayed === 0) {
      return '0%';
    }

    const percentage = (this.club.wins / this.club.matchesPlayed) * 100;

    return `${percentage.toFixed(1)}%`;
  }

  private getAverage(value: number): string {
    if (!this.club || this.club.matchesPlayed === 0) {
      return '0.00';
    }

    return (value / this.club.matchesPlayed).toFixed(2);
  }
}

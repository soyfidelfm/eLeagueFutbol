import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { finalize, switchMap } from 'rxjs';

import { ClubService } from '../../core/services/club.service';
import { CloudinaryService } from '../../core/services/cloudinary.service';
import { MyClubDto } from '../../core/models/my-club.model';

import {
  FORMATIONS,
  FORMATION_LAYOUTS,
  FormationPlayerPosition
} from '../../core/constants/formations';

type ClubPlayer = NonNullable<MyClubDto['players']>[number];

@Component({
  selector: 'app-clubs',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './clubs.html',
  styleUrl: './clubs.scss'
})
export class Clubs implements OnInit {

  club: MyClubDto | null = null;

  formations = FORMATIONS;
  formationLayouts = FORMATION_LAYOUTS;

  formationIndex = 0;
  modalFormationIndex = 0;

  selectedFormationLayout: FormationPlayerPosition[] = [];
  modalFormationLayout: FormationPlayerPosition[] = [];

  chemistry = 92;

  startingPlayers: ClubPlayer[] = [];
  modalStartingPlayers: ClubPlayer[] = [];

  isSquadModalOpen = false;
  selectedSlotIndex: number | null = null;

  isLoading = false;
  isUploadingLogo = false;
  errorMessage = '';

  constructor(
    private clubService: ClubService,
    private cloudinaryService: CloudinaryService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.updateFormation();
    this.loadMyClub();
  }

  get selectedFormation(): string {
    return this.formations[this.formationIndex];
  }

  get modalSelectedFormation(): string {
    return this.formations[this.modalFormationIndex];
  }

  nextFormation(): void {
    this.formationIndex =
      (this.formationIndex + 1) % this.formations.length;

    this.updateFormation();
  }

  previousFormation(): void {
    this.formationIndex =
      (this.formationIndex - 1 + this.formations.length) %
      this.formations.length;

    this.updateFormation();
  }

  nextModalFormation(): void {
    this.modalFormationIndex =
      (this.modalFormationIndex + 1) % this.formations.length;

    this.updateModalFormation();
  }

  previousModalFormation(): void {
    this.modalFormationIndex =
      (this.modalFormationIndex - 1 + this.formations.length) %
      this.formations.length;

    this.updateModalFormation();
  }

  private updateFormation(): void {
    this.selectedFormationLayout = this.getFormationLayout(this.selectedFormation);
    this.cdr.detectChanges();
  }

  private updateModalFormation(): void {
    this.modalFormationLayout = this.getFormationLayout(this.modalSelectedFormation);
    this.cdr.detectChanges();
  }

  private getFormationLayout(formation: string): FormationPlayerPosition[] {
    return [
      ...(this.formationLayouts[formation] ?? [])
    ];
  }

  loadMyClub(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.club = null;
    this.startingPlayers = [];
    this.modalStartingPlayers = [];
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
          this.startingPlayers = [...(response.players ?? [])].slice(0, 11);

          console.log('Club loaded:', response);
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
    this.selectedSlotIndex = null;

    this.modalFormationIndex = this.formationIndex;
    this.modalFormationLayout = this.getFormationLayout(this.selectedFormation);

    this.modalStartingPlayers = [
      ...this.startingPlayers
    ];

    this.cdr.detectChanges();
  }

  closeSquadModal(): void {
    this.isSquadModalOpen = false;
    this.selectedSlotIndex = null;

    this.modalStartingPlayers = [];
    this.modalFormationLayout = [];

    this.cdr.detectChanges();
  }

  selectSlot(index: number): void {
    this.selectedSlotIndex = index;
    this.cdr.detectChanges();
  }

  replacePlayer(player: ClubPlayer): void {
    if (this.selectedSlotIndex === null) {
      return;
    }

    this.modalStartingPlayers[this.selectedSlotIndex] = player;
    this.selectedSlotIndex = null;

    this.cdr.detectChanges();
  }

  saveLineup(): void {
  if (this.modalStartingPlayers.length !== 11) {
    this.errorMessage = 'Starting XI must have 11 players.';
    this.cdr.detectChanges();
    return;
  }

  const formation = this.modalSelectedFormation;
  const layout = this.getFormationLayout(formation);

  const payload = {
    formation,
    players: this.modalStartingPlayers.map((player, index) => ({
      playerId: player.playerId,
      slotIndex: index,
      position: layout[index]?.position ?? player.position
    }))
  };

  console.log('Sending lineup payload:', payload);

  this.clubService.updateMyClubLineup(payload)
    .pipe(
      finalize(() => {
        this.cdr.detectChanges();
      })
    )
    .subscribe({
      next: (updatedClub: MyClubDto) => {
        this.club = updatedClub;

        this.formationIndex = this.modalFormationIndex;
        this.selectedFormationLayout = layout;

        this.startingPlayers = [...this.modalStartingPlayers];

        this.closeSquadModal();

        console.log('Lineup saved:', updatedClub);
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
          this.cdr.detectChanges();
        },
        error: err => {
          console.error('Error uploading club logo', err);
          this.errorMessage = 'Unable to upload club logo.';
          this.cdr.detectChanges();
        }
      });
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

  get players() {
    return this.club?.players ?? [];
  }

  get activities() {
    return this.club?.activities ?? [];
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
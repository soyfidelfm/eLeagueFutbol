import { CommonModule } from '@angular/common';
import {
  Component,
  EventEmitter,
  Input,
  OnInit,
  Output
} from '@angular/core';

import {
  FORMATIONS,
  FORMATION_LAYOUTS
} from '../../../../core/constants/formations';

import {
  SquadModalPlayer,
  FormationSlot,
  SaveLineupPayload
} from '../../../../core/models/squad.model';

@Component({
  selector: 'app-squad-lineup-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './squad-lineup-modal.html',
  styleUrl: './squad-lineup-modal.scss'
})
export class SquadLineupModalComponent implements OnInit {

  @Input() players: SquadModalPlayer[] = [];
  @Input() startingPlayers: SquadModalPlayer[] = [];
  @Input() selectedFormation = '';
  @Input() formationLayout: FormationSlot[] = [];

  selectedSlotIndex: number | null = null;

  formations = FORMATIONS;
  formationLayouts = FORMATION_LAYOUTS;
  formationIndex = 0;

  @Output() close = new EventEmitter<void>();
  @Output() save = new EventEmitter<SaveLineupPayload>();

  ngOnInit(): void {
    const index = this.formations.findIndex(
      x => x === this.selectedFormation
    );

    this.formationIndex = index >= 0 ? index : 0;

    this.startingPlayers = [...this.startingPlayers]
  .sort((a, b) => (a.slotIndex ?? 0) - (b.slotIndex ?? 0));

    this.updateFormation();
  }

  previousFormation(): void {
    this.formationIndex =
      (this.formationIndex - 1 + this.formations.length) %
      this.formations.length;

    this.updateFormation();
  }

  nextFormation(): void {
    this.formationIndex =
      (this.formationIndex + 1) %
      this.formations.length;

    this.updateFormation();
  }

  private updateFormation(): void {
    this.selectedFormation = this.formations[this.formationIndex];

    this.formationLayout = [
      ...(this.formationLayouts[this.selectedFormation] ?? [])
    ];
  }

  selectSlot(index: number): void {
    this.selectedSlotIndex = index;
  }

  replacePlayer(player: SquadModalPlayer): void {
    if (this.selectedSlotIndex === null) {
      return;
    }

    const slotIndex = this.selectedSlotIndex;

    this.startingPlayers = [...this.startingPlayers];

    this.startingPlayers[slotIndex] = {
      ...player,
      slotIndex,
      position:
        this.formationLayout[slotIndex]?.position ||
        player.position ||
        'N/A'
    };

    this.selectedSlotIndex = null;
  }

  saveLineup(): void {
    const payload: SaveLineupPayload = {
      formation: this.selectedFormation,
      players: this.startingPlayers.map((player, index) => {
        const slotIndex = player.slotIndex ?? index;

        return {
          playerId: player.playerId,
          slotIndex,
          position:
            this.formationLayout[slotIndex]?.position ||
            player.position ||
            'N/A'
        };
      })
    };

    this.save.emit(payload);
  }

  onPlayerImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.src = '/images/avatar.png';
  }

  get canSave(): boolean {
    return this.startingPlayers.length === 11;
  }
}
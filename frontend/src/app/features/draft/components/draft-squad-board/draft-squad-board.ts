import { CommonModule } from '@angular/common';
import {
  Component,
  Input,
  OnChanges,
  SimpleChanges
} from '@angular/core';

import {
  DraftPick,
  DraftSquadPlayer
} from '../../../../core/models/draft.model';

interface SquadSlot {
  position: string;
  player?: DraftSquadPlayer;
}

interface PositionSummary {
  position: string;
  current: number;
  total: number;
  percent: number;
}

@Component({
  selector: 'app-draft-squad-board',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './draft-squad-board.html',
  styleUrl: './draft-squad-board.scss'
})
export class DraftSquadBoard implements OnChanges {

  @Input() players: DraftSquadPlayer[] = [];
  @Input() draftedCount = 0;
  @Input() currentBudget = 0;
  @Input() lastPicks: DraftPick[] = [];
  @Input() currentRound = 1;

  squadSlots: SquadSlot[] = [
    { position: 'ST' },
    { position: 'LW' },
    { position: 'RW' },
    { position: 'CM' },
    { position: 'CM' },
    { position: 'CDM' },
    { position: 'LB' },
    { position: 'CB' },
    { position: 'CB' },
    { position: 'RB' },
    { position: 'GK' }
  ];

  ngOnChanges(changes: SimpleChanges): void {

    if (changes['players']) {
      this.mapMySquadToSlots();
    }

  }

  get positionSummary(): PositionSummary[] {

    return this.squadSlots
      .reduce((summary, slot) => {

        let item = summary.find(x => x.position === slot.position);

        if (!item) {

          const total =
            this.squadSlots.filter(x => x.position === slot.position).length;

          item = {
            position: slot.position,
            current: 0,
            total,
            percent: 0
          };

          summary.push(item);
        }

        if (slot.player) {
          item.current++;
        }

        item.percent =
          (item.current / item.total) * 100;

        return summary;

      }, [] as PositionSummary[]);

  }

  trackBySlot(index: number): string {
    return `${this.squadSlots[index].position}-${index}`;
  }

  trackByPick(_: number, pick: DraftPick): number {
    return pick.pickNumber;
  }

  getPrimaryPosition(position: string): string {

    if (!position) {
      return '';
    }

    return position
      .split(',')[0]
      .trim();

  }
  private mapMySquadToSlots(): void {

    const template: SquadSlot[] = [
      { position: 'ST' },
      { position: 'LW' },
      { position: 'RW' },
      { position: 'CM' },
      { position: 'CM' },
      { position: 'CDM' },
      { position: 'LB' },
      { position: 'CB' },
      { position: 'CB' },
      { position: 'RB' },
      { position: 'GK' }
    ];

    const squadPlayers = [...(this.players ?? [])];

    for (const player of squadPlayers) {

      const primaryPosition =
        this.getPrimaryPosition(player.position);

      const exactSlot = template.find(slot =>
        !slot.player &&
        slot.position === primaryPosition
      );

      if (exactSlot) {

        exactSlot.player = player;

        continue;
      }

      const firstAvailable = template.find(slot => !slot.player);

      if (firstAvailable) {
        firstAvailable.player = player;
      }

    }

    this.squadSlots = template;

  }

}
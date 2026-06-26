import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

import { DraftOrder } from '../../../../core/models/draft.model';

@Component({
  selector: 'app-draft-order-board',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './draft-order-board.html',
  styleUrl: './draft-order-board.scss'
})
export class DraftOrderBoard {
  @Input() draftOrder: DraftOrder[] = [];
  @Input() currentRound = 1;
  @Input() playersPerTeam = 25;

  get currentRoundOrder(): DraftOrder[] {
    return this.draftOrder ?? [];
  }

  get nextRoundOrder(): DraftOrder[] {
    return [...(this.draftOrder ?? [])].reverse();
  }

  get hasDraftOrder(): boolean {
    return this.draftOrder.length > 0;
  }

  get currentPickTeam(): DraftOrder | null {
    return this.draftOrder.find(team => team.isCurrentPick) ?? null;
  }

  get currentPickClubName(): string {
    return this.currentPickTeam?.clubName ?? 'Waiting...';
  }

  get roundLabel(): string {
    return `Round ${this.currentRound} of ${this.playersPerTeam}`;
  }

  get nextRoundLabel(): string {
    return `Round ${this.currentRound + 1}`;
  }

  trackByUserId(index: number, team: DraftOrder): string {
    return team.userId || `${index}`;
  }
}
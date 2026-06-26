import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

import { DraftPlayer } from '../../../../../core/models/draft.model';

@Component({
  selector: 'app-draft-player-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './draft-player-card.html',
  styleUrl: './draft-player-card.scss'
})
export class DraftPlayerCardComponent {
  @Input({ required: true }) player!: DraftPlayer;

  @Input() canDraft = false;
  @Input() isPicking = false;
  @Input() currentBudget = 0;

  @Output() draft = new EventEmitter<DraftPlayer>();

  get canAfford(): boolean {
    return this.currentBudget >= (this.player.value ?? 0);
  }

  get isDisabled(): boolean {
    return !this.canDraft || this.isPicking || !this.canAfford;
  }

  get playerImage(): string {
    return this.player.imageUrl || 'assets/images/player-placeholder.png';
  }

  get price(): number {
    return this.player.value ?? 0;
  }

  get primaryPosition(): string {
    return this.player.position
      ? this.player.position.split(',')[0].trim()
      : '';
  }

  onDraft(): void {
    if (this.isDisabled) {
      return;
    }

    this.draft.emit(this.player);
  }
}
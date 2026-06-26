import { CommonModule } from '@angular/common';
import {
  Component,
  EventEmitter,
  Input,
  Output
} from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-draft-player-filters',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './draft-player-filters.html',
  styleUrl: './draft-player-filters.scss'
})
export class DraftPlayerFiltersComponent {

  @Input() searchTerm = '';
  @Input() selectedPosition = 'All';
  @Input() positions: string[] = [];

  @Output() searchTermChange =
    new EventEmitter<string>();

  @Output() positionChange =
    new EventEmitter<string>();

  @Output() search =
    new EventEmitter<void>();

  @Output() reload =
    new EventEmitter<void>();

  onSearchChange(value: string): void {
    this.searchTerm = value;
    this.searchTermChange.emit(value);
  }

  onPositionChange(position: string): void {
    if (this.selectedPosition === position) {
      return;
    }

    this.selectedPosition = position;
    this.positionChange.emit(position);
  }

  onSearch(): void {
    this.search.emit();
  }

  onReload(): void {
    this.reload.emit();
  }
}
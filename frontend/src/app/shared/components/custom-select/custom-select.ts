import {
  Component,
  EventEmitter,
  HostListener,
  Input,
  Output
} from '@angular/core';
import { CommonModule } from '@angular/common';

export interface SelectOption {
  value: string | number;
  label: string;
  icon?: string;
  imageUrl?: string;
}

@Component({
  selector: 'app-custom-select',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './custom-select.html',
  styleUrl: './custom-select.scss'
})
export class CustomSelect {

  @Input() placeholder = 'Select';
  @Input() items: SelectOption[] = [];
  @Input() value: string | number | null = null;
  @Input() disabled = false;

  @Output() valueChange =
    new EventEmitter<string | number>();

  isOpen = false;

  get selectedItem(): SelectOption | undefined {
    return this.items.find(x => x.value === this.value);
  }

  toggle(): void {
    if (this.disabled) {
      return;
    }

    this.isOpen = !this.isOpen;
  }

  select(item: SelectOption): void {
    this.value = item.value;

    this.valueChange.emit(item.value);

    this.isOpen = false;
  }

  @HostListener('document:click')
  close(): void {
    this.isOpen = false;
  }

  stopPropagation(event: Event): void {
    event.stopPropagation();
  }
}
import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

export interface NeonDropdownOption {
  label: string;
  value: string;
}

@Component({
  selector: 'app-neon-dropdown',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './neon-dropdown.html',
  styleUrl: './neon-dropdown.scss'
})
export class NeonDropdownComponent {
  @Input() label = '';
  @Input() placeholder = 'Select option';
  @Input() options: NeonDropdownOption[] = [];
  @Input() selectedValue: string | null = null;

  @Output() selectedValueChange = new EventEmitter<string>();

  isOpen = false;

  get selectedOption(): NeonDropdownOption | null {
    return this.options.find(x => x.value === this.selectedValue) ?? null;
  }

  toggle(): void {
    this.isOpen = !this.isOpen;
  }

  select(option: NeonDropdownOption): void {
    this.selectedValue = option.value;
    this.selectedValueChange.emit(option.value);
    this.isOpen = false;
  }
}
import { AfterViewChecked, Component, ElementRef, HostListener, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';

import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { ConfirmDialogService } from './confirm-dialog.service';

/** Mount once (in the Shell). Driven by ConfirmDialogService. */
@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './confirm-dialog.html',
  styleUrl: './confirm-dialog.scss'
})
export class ConfirmDialog implements AfterViewChecked {
  readonly dialog = inject(ConfirmDialogService);

  @ViewChild('cancelButton') cancelButton?: ElementRef<HTMLButtonElement>;

  private focusedFor: unknown = null;

  ngAfterViewChecked(): void {
    const state = this.dialog.state();

    // Focus Cancel once per opened dialog (safe default for destructive actions).
    if (state && this.focusedFor !== state && this.cancelButton) {
      this.focusedFor = state;
      setTimeout(() => this.cancelButton?.nativeElement.focus());
    }

    if (!state) this.focusedFor = null;
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.dialog.state()) this.dialog.cancel();
  }
}

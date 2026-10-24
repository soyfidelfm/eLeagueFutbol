import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

import { TranslatePipe } from '../../../core/i18n/translate.pipe';

/**
 * Reusable empty state: icon + title + short description, with an optional
 * projected action (e.g. a button). Title/description are English keys and
 * are translated here with the `t` pipe.
 *
 * <app-empty-state icon="fa-solid fa-futbol" title="No matches" description="...">
 *   <button>...</button>
 * </app-empty-state>
 */
@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './empty-state.html',
  styleUrl: './empty-state.scss'
})
export class EmptyState {
  /** Font Awesome classes, e.g. "fa-solid fa-inbox". */
  @Input() icon = 'fa-regular fa-folder-open';
  @Input() title = '';
  @Input() description = '';
  /** Smaller variant for cards / side panels. */
  @Input() compact = false;
  /** Red-tinted variant for errors. */
  @Input() tone: 'default' | 'error' = 'default';
}

import { Pipe, PipeTransform, inject } from '@angular/core';

import { TranslationService } from './translation.service';

/** Usage: {{ 'Save changes' | t }} or {{ '{n} players' | t: { n: count } }} */
@Pipe({
  name: 't',
  standalone: true,
  pure: false
})
export class TranslatePipe implements PipeTransform {
  private readonly i18n = inject(TranslationService);

  transform(value: string | null | undefined, params?: Record<string, unknown>): string {
    return this.i18n.t(value, params);
  }
}

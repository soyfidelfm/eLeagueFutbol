import { Pipe, PipeTransform, inject } from '@angular/core';
import { formatDate } from '@angular/common';

import { TranslationService } from './translation.service';
import { getAppTimeZone, timeZoneOffset } from './timezone';

/**
 * Drop-in replacement for the `date` pipe that uses the current UI
 * language for locale and the user's settings time zone.
 * An explicit timezone argument still wins.
 */
@Pipe({
  name: 'tdate',
  standalone: true,
  pure: false
})
export class LocalizedDatePipe implements PipeTransform {
  private readonly i18n = inject(TranslationService);

  transform(
    value: string | number | Date | null | undefined,
    format = 'mediumDate',
    timezone?: string
  ): string | null {
    if (value === null || value === undefined || value === '') return null;

    // Backend DateTimes without a zone suffix are UTC.
    const input = typeof value === 'string' && /T\d{2}:\d{2}/.test(value) && !/(Z|[+-]\d{2}:?\d{2})$/.test(value)
      ? `${value}Z`
      : value;

    const date = input instanceof Date ? input : new Date(input);
    if (isNaN(date.getTime())) return null;

    const zone = timezone ?? timeZoneOffset(date, getAppTimeZone());

    return formatDate(date, format, this.i18n.locale, zone);
  }
}

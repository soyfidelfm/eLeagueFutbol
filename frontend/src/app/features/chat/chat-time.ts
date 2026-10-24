const DAY = 86_400_000;

function startOfDay(d: Date): number {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
}

/** Conversation list / notification time: "14:05" today, weekday this week, else short date. */
export function shortTime(iso: string, locale: string, now = new Date()): string {
  const d = new Date(iso);
  if (isNaN(d.getTime())) return '';
  const days = Math.round((startOfDay(now) - startOfDay(d)) / DAY);
  if (days <= 0) return new Intl.DateTimeFormat(locale, { hour: '2-digit', minute: '2-digit' }).format(d);
  if (days < 7) return new Intl.DateTimeFormat(locale, { weekday: 'short' }).format(d);
  return new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short' }).format(d);
}

/** Bubble time: always hh:mm. */
export function clockTime(iso: string, locale: string): string {
  const d = new Date(iso);
  return isNaN(d.getTime()) ? '' : new Intl.DateTimeFormat(locale, { hour: '2-digit', minute: '2-digit' }).format(d);
}

/** Days between `iso` and today (0 = today, 1 = yesterday). */
export function daysAgo(iso: string, now = new Date()): number {
  return Math.round((startOfDay(now) - startOfDay(new Date(iso))) / DAY);
}

/** Long date for day separators. */
export function longDate(iso: string, locale: string): string {
  return new Intl.DateTimeFormat(locale, { weekday: 'long', day: 'numeric', month: 'long' }).format(new Date(iso));
}

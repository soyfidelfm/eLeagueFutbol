/** Current user time zone (IANA id). Updated by SettingsService. */
let currentTimeZone: string | null = null;

export function setAppTimeZone(timeZone: string | null | undefined): void {
  currentTimeZone = timeZone && isValidTimeZone(timeZone) ? timeZone : null;
}

export function getAppTimeZone(): string {
  return currentTimeZone ?? browserTimeZone();
}

export function browserTimeZone(): string {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';
  } catch {
    return 'UTC';
  }
}

export function isValidTimeZone(timeZone: string): boolean {
  try {
    new Intl.DateTimeFormat('en-US', { timeZone });
    return true;
  } catch {
    return false;
  }
}

export function listTimeZones(): string[] {
  const intl = Intl as unknown as { supportedValuesOf?: (key: string) => string[] };
  const zones = intl.supportedValuesOf?.('timeZone') ?? [];
  const all = zones.length ? [...zones] : ['UTC'];
  if (!all.includes('UTC')) all.unshift('UTC');
  return all;
}

/** Offset string like "-0700" for Angular's formatDate, for a given instant. */
export function timeZoneOffset(date: Date, timeZone: string): string {
  try {
    const parts = new Intl.DateTimeFormat('en-US', {
      timeZone,
      hourCycle: 'h23',
      year: 'numeric', month: '2-digit', day: '2-digit',
      hour: '2-digit', minute: '2-digit', second: '2-digit'
    }).formatToParts(date);

    const get = (type: string) => Number(parts.find(p => p.type === type)?.value ?? 0);
    const asUtc = Date.UTC(get('year'), get('month') - 1, get('day'), get('hour'), get('minute'), get('second'));
    const minutes = Math.round((asUtc - Math.floor(date.getTime() / 1000) * 1000) / 60000);

    const sign = minutes >= 0 ? '+' : '-';
    const abs = Math.abs(minutes);
    return `${sign}${String(Math.floor(abs / 60)).padStart(2, '0')}${String(abs % 60).padStart(2, '0')}`;
  } catch {
    return '+0000';
  }
}

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';

import { API_ENDPOINTS } from '../constants/api-endpoints';
import { TranslationService } from '../i18n/translation.service';
import { browserTimeZone, setAppTimeZone } from '../i18n/timezone';

export interface UserSettings {
  timeZone: string;
  language: string;
  pushNotificationsEnabled: boolean;
  inAppToastsEnabled: boolean;
}

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly http = inject(HttpClient);
  private readonly i18n = inject(TranslationService);

  private readonly settingsSubject = new BehaviorSubject<UserSettings | null>(null);
  readonly settings$ = this.settingsSubject.asObservable();

  private readonly url = `${API_ENDPOINTS.users}/me/settings`;

  get current(): UserSettings | null {
    return this.settingsSubject.value;
  }

  /** Toasts are on unless the user explicitly turned them off. */
  get toastsEnabled(): boolean {
    return this.current?.inAppToastsEnabled !== false;
  }

  /** Loads from the backend and reconciles language / time zone. */
  load(): Observable<UserSettings> {
    return this.http.get<UserSettings>(this.url).pipe(
      tap(settings => this.apply(settings))
    );
  }

  save(settings: UserSettings): Observable<UserSettings> {
    return this.http.put<UserSettings>(this.url, settings).pipe(
      tap(saved => this.apply(saved))
    );
  }

  private apply(settings: UserSettings): void {
    // An unset/"UTC" default from registration falls back to the browser zone for display.
    const timeZone = settings.timeZone?.trim() ? settings.timeZone : browserTimeZone();
    setAppTimeZone(timeZone);
    this.i18n.setLanguage(settings.language);
    this.settingsSubject.next({ ...settings, timeZone });
  }
}

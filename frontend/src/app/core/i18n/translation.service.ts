import { Injectable, signal } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeEs from '@angular/common/locales/es';

import { ES } from './es';

export type AppLanguage = 'en' | 'es';

export const SUPPORTED_LANGUAGES: { code: AppLanguage; label: string }[] = [
  { code: 'en', label: 'English' },
  { code: 'es', label: 'Español' }
];

const STORAGE_KEY = 'eleague.language';

const DICTIONARIES: Record<AppLanguage, Record<string, string>> = {
  // English is the source language: keys are the English text itself.
  en: {},
  es: ES
};

registerLocaleData(localeEs, 'es');

/**
 * Minimal i18n: keys are the English source strings, so a missing
 * translation always falls back to readable English.
 */
@Injectable({ providedIn: 'root' })
export class TranslationService {
  readonly language = signal<AppLanguage>(TranslationService.readStored());

  constructor() {
    document.documentElement.lang = this.language();
  }

  static isSupported(value: unknown): value is AppLanguage {
    return value === 'en' || value === 'es';
  }

  private static readStored(): AppLanguage {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      return TranslationService.isSupported(stored) ? stored : 'en';
    } catch {
      return 'en';
    }
  }

  /** Angular locale id for date/number formatting. */
  get locale(): string {
    return this.language() === 'es' ? 'es' : 'en-US';
  }

  setLanguage(language: string | null | undefined): void {
    const next: AppLanguage = TranslationService.isSupported(language) ? language : 'en';

    this.language.set(next);
    document.documentElement.lang = next;

    try {
      localStorage.setItem(STORAGE_KEY, next);
    } catch {
      // storage unavailable: language still applies for this session
    }
  }

  t(key: string | null | undefined, params?: Record<string, unknown>): string {
    if (key === null || key === undefined || key === '') return '';

    const text = DICTIONARIES[this.language()][key] ?? key;

    if (!params) return text;

    return text.replace(/\{(\w+)\}/g, (match, name) =>
      params[name] !== undefined && params[name] !== null ? String(params[name]) : match
    );
  }
}

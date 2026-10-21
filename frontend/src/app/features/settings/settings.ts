import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { SettingsService, UserSettings } from '../../core/services/settings.service';
import { SUPPORTED_LANGUAGES, TranslationService } from '../../core/i18n/translation.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { browserTimeZone, listTimeZones } from '../../core/i18n/timezone';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './settings.html',
  styleUrl: './settings.scss'
})
export class Settings implements OnInit {
  private readonly settingsService = inject(SettingsService);
  private readonly i18n = inject(TranslationService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly languages = SUPPORTED_LANGUAGES;
  readonly timeZones = listTimeZones();
  readonly browserZone = browserTimeZone();

  model: UserSettings = {
    timeZone: browserTimeZone(),
    language: this.i18n.language(),
    pushNotificationsEnabled: true,
    inAppToastsEnabled: true
  };

  isLoading = true;
  isSaving = false;
  successMessage = '';
  errorMessage = '';

  ngOnInit(): void {
    this.settingsService.load().subscribe({
      next: settings => {
        this.model = { ...settings };
        if (!this.timeZones.includes(this.model.timeZone)) {
          this.timeZones.unshift(this.model.timeZone);
        }
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Could not load your settings.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  /** Language applies immediately and is persisted right away. */
  onLanguageChange(language: string): void {
    this.model.language = language;
    this.i18n.setLanguage(language);
    this.save();
  }

  useBrowserZone(): void {
    this.model.timeZone = this.browserZone;
  }

  save(): void {
    if (this.isSaving) return;

    this.isSaving = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.settingsService.save(this.model).subscribe({
      next: saved => {
        this.model = { ...saved };
        this.isSaving = false;
        this.successMessage = 'Settings saved.';
        this.cdr.detectChanges();
      },
      error: err => {
        this.isSaving = false;
        this.errorMessage = err?.error?.message || 'Could not save your settings.';
        this.cdr.detectChanges();
      }
    });
  }
}

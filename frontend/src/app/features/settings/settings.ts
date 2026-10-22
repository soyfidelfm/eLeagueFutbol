import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { SettingsService, UserSettings } from '../../core/services/settings.service';
import { SUPPORTED_LANGUAGES, TranslationService } from '../../core/i18n/translation.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { browserTimeZone, listTimeZones } from '../../core/i18n/timezone';
import { UserService } from '../../core/services/user.service';
import { DraftService } from '../../core/services/draft.service';
import { take } from 'rxjs';

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
  private readonly userService = inject(UserService);
  private readonly draftService = inject(DraftService);

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

  /** Admin-only draft defaults. */
  isAdmin = false;
  draftPickSeconds = 300;
  draftMinSeconds = 10;
  draftMaxSeconds = 600;
  isSavingDraft = false;
  draftSuccessMessage = '';
  draftErrorMessage = '';

  get isDraftSecondsValid(): boolean {
    const value = Number(this.draftPickSeconds);
    return Number.isInteger(value) && value >= this.draftMinSeconds && value <= this.draftMaxSeconds;
  }

  ngOnInit(): void {
    this.userService.getMe().pipe(take(1)).subscribe({
      next: me => {
        this.isAdmin = (me?.role ?? '').toLowerCase() === 'admin';
        if (this.isAdmin) {
          this.loadDraftSettings();
        }
        this.cdr.detectChanges();
      }
    });

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

  private loadDraftSettings(): void {
    this.draftService.getSettings().subscribe({
      next: settings => {
        this.draftPickSeconds = settings.defaultPickSeconds;
        this.draftMinSeconds = settings.minPickSeconds || 10;
        this.draftMaxSeconds = settings.maxPickSeconds || 600;
        this.cdr.detectChanges();
      },
      error: () => {
        this.draftErrorMessage = 'Could not load draft settings.';
        this.cdr.detectChanges();
      }
    });
  }

  saveDraftSettings(): void {
    if (this.isSavingDraft) return;

    this.draftSuccessMessage = '';
    this.draftErrorMessage = '';

    if (!this.isDraftSecondsValid) {
      this.draftErrorMessage = 'Seconds per pick must be between 10 and 600.';
      return;
    }

    this.isSavingDraft = true;

    this.draftService.updateSettings({ defaultPickSeconds: Number(this.draftPickSeconds) }).subscribe({
      next: saved => {
        this.draftPickSeconds = saved.defaultPickSeconds;
        this.isSavingDraft = false;
        this.draftSuccessMessage = 'Draft settings saved.';
        this.cdr.detectChanges();
      },
      error: err => {
        this.isSavingDraft = false;
        this.draftErrorMessage = err?.error?.message || 'Could not save draft settings.';
        this.cdr.detectChanges();
      }
    });
  }
}

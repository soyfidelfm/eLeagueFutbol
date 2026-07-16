import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';

import {
  MatchStatsService
} from '../../../core/services/match-stats.service';

import {
  MatchStatsUploadType,
  MatchStatsUploadResult
} from '../../../core/models/match-stats.model';

@Component({
  selector: 'app-screenshot-uploader',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './screenshot-uploader.html',
  styleUrl: './screenshot-uploader.scss'
})
export class ScreenshotUploader {

  matchId = '';

  selectedFile: File | null = null;
  previewImageUrl: string | null = null;

  uploadType: MatchStatsUploadType = 'MatchSummary';

  isLoading = false;
  errorMessage = '';

  uploadResult: MatchStatsUploadResult | null = null;

  constructor(
    private matchStatsService: MatchStatsService
  ) {}

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;

    if (!input.files || input.files.length === 0) {
      return;
    }

    this.selectedFile = input.files[0];
    this.uploadResult = null;
    this.errorMessage = '';

    this.previewImageUrl = URL.createObjectURL(this.selectedFile);
  }

  onUploadTypeChange(type: MatchStatsUploadType): void {
    this.uploadType = type;
    this.uploadResult = null;
    this.errorMessage = '';
  }

  readScreenshot(): void {
    if (!this.matchId) {
      this.errorMessage = 'Missing match id.';
      return;
    }

    if (!this.selectedFile) {
      this.errorMessage = 'Please select a screenshot first.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.matchStatsService
      .uploadStatsImage(this.matchId, this.uploadType, this.selectedFile)
      .subscribe({
        next: response => {
          this.uploadResult = response;
          this.isLoading = false;
        },
        error: err => {
          console.error('OCR upload error:', err);
          this.errorMessage = 'Could not read screenshot.';
          this.isLoading = false;
        }
      });
  }
}
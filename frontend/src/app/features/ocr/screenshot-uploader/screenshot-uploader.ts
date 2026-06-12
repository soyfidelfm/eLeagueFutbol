import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';

import { MatchStatsService } from '../../../core/services/match-stats.service';
import {
  MatchPlayerStatsOcrPreview
} from '../../../core/models/match-player-stats-ocr.model';

@Component({
  selector: 'app-screenshot-uploader',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './screenshot-uploader.html',
  styleUrl: './screenshot-uploader.scss'
})
export class ScreenshotUploader {

  selectedFile: File | null = null;
  previewImageUrl: string | null = null;

  isLoading = false;
  errorMessage = '';

  ocrPreview: MatchPlayerStatsOcrPreview | null = null;

  constructor(
    private matchStatsService: MatchStatsService
  ) {}

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;

    if (!input.files || input.files.length === 0) {
      return;
    }

    this.selectedFile = input.files[0];
    this.ocrPreview = null;
    this.errorMessage = '';

    this.previewImageUrl = URL.createObjectURL(this.selectedFile);
  }

  readScreenshot(): void {
    if (!this.selectedFile) {
      this.errorMessage = 'Please select a screenshot first.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.matchStatsService.previewPlayerStats(this.selectedFile).subscribe({
      next: response => {
        console.log('OCR preview:', response);
        this.ocrPreview = response;
        this.isLoading = false;
      },
      error: err => {
        console.error('OCR error:', err);
        this.errorMessage = 'Could not read screenshot.';
        this.isLoading = false;
      }
    });
  }
}
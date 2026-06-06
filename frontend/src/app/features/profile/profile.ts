import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { User } from '../../core/models/user.model';
import { UserService } from '../../core/services/user.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.html',
  styleUrl: './profile.scss'
})
export class Profile implements OnInit {
  user: User | null = null;

  isLoading = false;
  isSaving = false;

  errorMessage = '';
  successMessage = '';

  form = {
    displayName: '',
    avatarUrl: '',
    timeZone: ''
  };

  constructor(private userService: UserService) {}

  ngOnInit(): void {
    this.loadUser();
  }

  loadUser(): void {
    const userId = localStorage.getItem('userId');

    if (!userId) {
      this.errorMessage = 'User not found. Please login again.';
      return;
    }

    this.isLoading = true;

    this.userService.getById(userId).subscribe({
      next: user => {
        this.user = user;

        this.form = {
          displayName: user.displayName,
          avatarUrl: user.avatarUrl,
          timeZone: user.timeZone
        };

        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Error loading profile.';
        this.isLoading = false;
      }
    });
  }

  save(): void {
    if (!this.user) return;

    this.isSaving = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.userService.update(this.user.userId, this.form).subscribe({
      next: user => {
        this.user = user;
        this.successMessage = 'Profile updated.';
        this.isSaving = false;
      },
      error: () => {
        this.errorMessage = 'Error updating profile.';
        this.isSaving = false;
      }
    });
  }
}
import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { Navbar } from '../navbar/navbar';
import { Sidebar } from '../sidebar/sidebar';
import { Footer } from '../footer/footer';

import { NotificationService } from '../../core/services/notification.service';
import { NotificationApiService } from '../../core/services/notification-api.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    RouterOutlet,
    Navbar,
    Sidebar,
    Footer
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss'
})
export class Shell implements OnInit {

  constructor(
    private notificationService: NotificationService,
    private notificationApiService: NotificationApiService
  ) { }

  async ngOnInit(): Promise<void> {
    this.notificationService.listenForMessages();

    const userSynced = localStorage.getItem('userSynced') === 'true';
    const userId = localStorage.getItem('userId');

    if (!userSynced || !userId) {
      return;
    }

    const token =
      await this.notificationService.requestPermissionAndGetToken();

    if (!token) {
      return;
    }

    this.notificationApiService.saveToken(token)
      .subscribe({
        error: error => {
          console.error('Error saving notification token:', error);
        }
      });
  }
}
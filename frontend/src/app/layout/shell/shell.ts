import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { filter, switchMap, take } from 'rxjs';

import { Navbar } from '../navbar/navbar';
import { Sidebar } from '../sidebar/sidebar';
import { Footer } from '../footer/footer';

import { NotificationService } from '../../core/services/notification.service';
import { NotificationInboxService } from '../../core/services/notification-inbox.service';
import { RealtimeService } from '../../core/services/realtime.service';

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
    public auth: AuthService,
    private notificationService: NotificationService,
    private notificationInboxService: NotificationInboxService,
    private realtimeService: RealtimeService
  ) { }

  async ngOnInit(): Promise<void> {

    this.notificationService.listenForMessages();

    this.auth.isAuthenticated$
      .pipe(
        filter(isAuthenticated => isAuthenticated),
        take(1),
        switchMap(() => this.auth.getAccessTokenSilently())
      )
      .subscribe({
        next: token => {
          this.realtimeService.start(token);
        },
        error: error => {
          console.error(
            'Error starting realtime connection:',
            error
          );
        }
      });

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

    this.notificationInboxService.saveToken({
      token,
      deviceType: 'web'
    })
      .subscribe({
        error: error => {
          console.error(
            'Error saving notification token:',
            error
          );
        }
      });
  }
}
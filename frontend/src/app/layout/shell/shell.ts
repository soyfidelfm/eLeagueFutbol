import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { Navbar } from '../navbar/navbar';
import { Sidebar } from '../sidebar/sidebar';
import { Footer } from '../footer/footer';

import { NotificationService } from '../../core/services/notification.service';
import { NotificationInboxService } from '../../core/services/notification-inbox.service';
import { RealtimeService } from '../../core/services/realtime.service';
import { UserService } from '../../core/services/user.service';

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
    private notificationInboxService: NotificationInboxService,
    private realtime: RealtimeService,
    private userService: UserService
  ) { }

  async ngOnInit(): Promise<void> {

    this.notificationService.listenForMessages();

    this.userService.getMe().subscribe({
      next: me => {
        if (me?.userId) {
          this.realtime.setCurrentUserId(me.userId);
          localStorage.setItem('userId', me.userId);
          localStorage.setItem('userSynced', 'true');
        }

        void this.realtime.connect();
      },
      error: () => {
        void this.realtime.connect();
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

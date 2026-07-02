import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { Subscription, take } from 'rxjs';

import { MessageService } from '../../core/services/message.service';
import { AppLoading } from '../../shared/components/app-loading/app-loading';

@Component({
  selector: 'app-message-notifications',
  standalone: true,
  imports: [CommonModule, RouterModule, AppLoading],
  templateUrl: './message-notifications.html',
  styleUrl: './message-notifications.scss'
})
export class MessageNotifications implements OnInit, OnDestroy {
  unreadMessagesCount = 0;

  private subscription = new Subscription();
  private receivedListener?: EventListener;
  private readListener?: EventListener;

  constructor(
    private messageService: MessageService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadUnreadMessagesCount();

    this.receivedListener = () => {
      this.loadUnreadMessagesCount();
    };

    this.readListener = () => {
      this.loadUnreadMessagesCount();
    };

    window.addEventListener('eleague-message-received', this.receivedListener);
    window.addEventListener('eleague-message-read', this.readListener);
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();

    if (this.receivedListener) {
      window.removeEventListener('eleague-message-received', this.receivedListener);
    }

    if (this.readListener) {
      window.removeEventListener('eleague-message-read', this.readListener);
    }
  }

  loadUnreadMessagesCount(): void {
    const sub = this.messageService.getConversations()
      .pipe(take(1))
      .subscribe({
        next: conversations => {
  console.log('MESSAGE CONVERSATIONS:', conversations);

  this.unreadMessagesCount = (conversations ?? []).reduce(
    (total, item) => total + Number(item.unreadCount ?? 0),
    0
  );

  console.log('MESSAGE BADGE COUNT:', this.unreadMessagesCount);

  this.cdr.detectChanges();
},
        error: error => {
          console.error('Error loading unread messages:', error);
          this.unreadMessagesCount = 0;
          this.cdr.detectChanges();
        }
      });

    this.subscription.add(sub);
  }
}
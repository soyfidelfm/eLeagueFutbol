import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize, forkJoin, Subscription } from 'rxjs';

import { MessageService } from '../../core/services/message.service';
import { UserService } from '../../core/services/user.service';
import { AppLoading } from '../../shared/components/app-loading/app-loading';

import {
  ConversationDto,
  MessageDto,
  SendMessageDto
} from '../../core/models/message.model';

interface MessageUserResult {
  userId: string;
  displayName: string;
  avatarUrl?: string | null;
}

@Component({
  selector: 'app-messages',
  standalone: true,
  imports: [CommonModule, FormsModule, AppLoading],
  templateUrl: './messages.html',
  styleUrl: './messages.scss'
})
export class Messages implements OnInit, OnDestroy {

  isLoading = false;
  isLoadingConversation = false;
  isSending = false;

  errorMessage = '';
  conversationSearchTerm = '';

  conversations: ConversationDto[] = [];
  messages: MessageDto[] = [];

  selectedUserId = '';
  selectedConversation: ConversationDto | null = null;

  newMessageText = '';

  showNewMessageModal = false;
  userSearchTerm = '';
  isSearchingUsers = false;
  userResults: MessageUserResult[] = [];

  private messageListener?: EventListener;
  private subscription = new Subscription();

  constructor(
    private messageService: MessageService,
    private userService: UserService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadConversations();

    this.messageListener = () => {
      this.loadConversations();

      if (this.selectedUserId) {
        this.loadConversation(this.selectedUserId);
      }

      this.cdr.detectChanges();
    };

    window.addEventListener(
      'eleague-message-received',
      this.messageListener
    );
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();

    if (this.messageListener) {
      window.removeEventListener(
        'eleague-message-received',
        this.messageListener
      );
    }
  }

  get filteredConversations(): ConversationDto[] {
    const term = this.conversationSearchTerm.trim().toLowerCase();

    if (!term) {
      return this.conversations;
    }

    return this.conversations.filter(item =>
      item.displayName.toLowerCase().includes(term) ||
      item.lastMessage.toLowerCase().includes(term)
    );
  }

  loadConversations(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    const sub = this.messageService.getConversations()
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: response => {
          this.conversations = response ?? [];

          if (this.selectedUserId) {
            const selected = this.conversations.find(x =>
              x.userId === this.selectedUserId
            );

            if (selected) {
              this.selectedConversation = selected;
            }
          }

          this.cdr.detectChanges();
        },
        error: error => {
          this.errorMessage =
            error?.error?.message || 'Unable to load conversations.';

          this.conversations = [];
          this.cdr.detectChanges();
        }
      });

    this.subscription.add(sub);
  }

  openConversation(item: ConversationDto): void {
    this.selectedConversation = item;
    this.selectedUserId = item.userId;
    this.messages = [];
    this.errorMessage = '';

    item.unreadCount = 0;

    this.cdr.detectChanges();

    this.loadConversation(item.userId);
  }

  loadConversation(userId: string): void {
    this.isLoadingConversation = true;
    this.cdr.detectChanges();

    const sub = this.messageService.getConversation(userId)
      .pipe(
        finalize(() => {
          this.isLoadingConversation = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: response => {
          this.messages = response ?? [];

          const unreadMessages = this.messages.filter(x =>
            !x.isMine &&
            !x.isRead
          );

          if (unreadMessages.length === 0) {
            this.loadConversations();
            this.cdr.detectChanges();
            return;
          }

          const markReadRequests = unreadMessages.map(message =>
            this.messageService.markAsRead(message.messageId)
          );

          const readSub = forkJoin(markReadRequests)
            .subscribe({
              next: () => {
                this.messages = this.messages.map(message => ({
                  ...message,
                  isRead: true
                }));

                if (this.selectedConversation) {
                  this.selectedConversation.unreadCount = 0;
                }

                this.loadConversations();

                window.dispatchEvent(
                  new CustomEvent('eleague-message-read')
                );

                this.cdr.detectChanges();
              },
              error: error => {
                console.error('Error marking messages as read:', error);
              }
            });

          this.subscription.add(readSub);
        },
        error: error => {
          this.errorMessage =
            error?.error?.message || 'Unable to load conversation.';

          this.messages = [];
          this.cdr.detectChanges();
        }
      });

    this.subscription.add(sub);
  }

  sendMessage(): void {
    const text = this.newMessageText.trim();

    if (!text || !this.selectedUserId || this.isSending) {
      return;
    }

    const dto: SendMessageDto = {
      receiverUserId: this.selectedUserId,
      text
    };

    this.isSending = true;
    this.cdr.detectChanges();

    const sub = this.messageService.sendMessage(dto)
      .pipe(
        finalize(() => {
          this.isSending = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: message => {
          this.messages = [
            ...this.messages,
            message
          ];

          this.newMessageText = '';

          this.upsertConversationAfterSend(message);
          this.loadConversations();

          this.cdr.detectChanges();
        },
        error: error => {
          this.errorMessage =
            error?.error?.message || 'Unable to send message.';

          this.cdr.detectChanges();
        }
      });

    this.subscription.add(sub);
  }

  sendOnEnter(event: Event): void {
    const keyboardEvent = event as KeyboardEvent;

    if (keyboardEvent.shiftKey) {
      return;
    }

    keyboardEvent.preventDefault();
    this.sendMessage();
  }

  startNewMessage(): void {
    this.showNewMessageModal = true;
    this.userSearchTerm = '';
    this.userResults = [];
    this.errorMessage = '';
    this.cdr.detectChanges();
  }

  closeNewMessageModal(): void {
    this.showNewMessageModal = false;
    this.userSearchTerm = '';
    this.userResults = [];
    this.isSearchingUsers = false;
    this.cdr.detectChanges();
  }

  searchUsers(): void {
    const term = this.userSearchTerm.trim();

    if (term.length < 4) {
      this.userResults = [];
      this.cdr.detectChanges();
      return;
    }

    this.isSearchingUsers = true;
    this.cdr.detectChanges();

    const sub = this.userService.searchUsers(term)
      .pipe(
        finalize(() => {
          this.isSearchingUsers = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: response => {
          this.userResults = response ?? [];
          this.cdr.detectChanges();
        },
        error: error => {
          console.error('Error searching users', error);
          this.userResults = [];
          this.cdr.detectChanges();
        }
      });

    this.subscription.add(sub);
  }

  startConversationWith(user: MessageUserResult): void {
    const existingConversation = this.conversations.find(x =>
      x.userId === user.userId
    );

    if (existingConversation) {
      this.openConversation(existingConversation);
      this.closeNewMessageModal();
      return;
    }

    this.selectedUserId = user.userId;

    this.selectedConversation = {
      userId: user.userId,
      displayName: user.displayName,
      avatarUrl: user.avatarUrl,
      lastMessage: '',
      lastMessageAtUTC: new Date().toISOString(),
      unreadCount: 0
    };

    this.messages = [];
    this.newMessageText = '';
    this.errorMessage = '';

    this.closeNewMessageModal();
    this.cdr.detectChanges();
  }

  private upsertConversationAfterSend(message: MessageDto): void {
    if (!this.selectedConversation) {
      return;
    }

    const existing = this.conversations.find(x =>
      x.userId === this.selectedConversation?.userId
    );

    if (existing) {
      existing.lastMessage = message.text;
      existing.lastMessageAtUTC = message.sentAtUTC;
      existing.unreadCount = 0;
      this.cdr.detectChanges();
      return;
    }

    this.conversations = [
      {
        userId: this.selectedConversation.userId,
        displayName: this.selectedConversation.displayName,
        avatarUrl: this.selectedConversation.avatarUrl,
        lastMessage: message.text,
        lastMessageAtUTC: message.sentAtUTC,
        unreadCount: 0
      },
      ...this.conversations
    ];

    this.cdr.detectChanges();
  }
}
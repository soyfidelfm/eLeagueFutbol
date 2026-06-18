import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';

import { MessageService } from '../../core/services/message.service';
import { UserService } from '../../core/services/user.service';

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
  imports: [CommonModule, FormsModule],
  templateUrl: './messages.html',
  styleUrl: './messages.scss'
})
export class Messages implements OnInit {

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

  constructor(
    private messageService: MessageService,
    private userService: UserService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadConversations();
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

    this.messageService.getConversations()
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: response => {
          this.conversations = response;
        },
        error: error => {
          this.errorMessage =
            error?.error?.message || 'Unable to load conversations.';
        }
      });
  }

  openConversation(item: ConversationDto): void {
    this.selectedConversation = item;
    this.selectedUserId = item.userId;
    this.messages = [];
    this.errorMessage = '';

    this.loadConversation(item.userId);
  }

  loadConversation(userId: string): void {
    this.isLoadingConversation = true;

    this.messageService.getConversation(userId)
      .pipe(
        finalize(() => {
          this.isLoadingConversation = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: response => {
          this.messages = response;

          const unreadMessages = response.filter(x =>
            !x.isMine &&
            !x.isRead
          );

          unreadMessages.forEach(message => {
            this.messageService.markAsRead(message.messageId).subscribe();
          });

          if (this.selectedConversation) {
            this.selectedConversation.unreadCount = 0;
          }
        },
        error: error => {
          this.errorMessage =
            error?.error?.message || 'Unable to load conversation.';
        }
      });
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

    this.messageService.sendMessage(dto)
      .pipe(
        finalize(() => {
          this.isSending = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: message => {
          this.messages.push(message);
          this.newMessageText = '';

          this.upsertConversationAfterSend(message);
          this.loadConversations();
        },
        error: error => {
          this.errorMessage =
            error?.error?.message || 'Unable to send message.';
        }
      });
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

    setTimeout(() => {
      this.cdr.detectChanges();
    });
  }

  closeNewMessageModal(): void {
    this.showNewMessageModal = false;
    this.userSearchTerm = '';
    this.userResults = [];
    this.isSearchingUsers = false;
  }

  searchUsers(): void {
  const term = this.userSearchTerm.trim();

  if (term.length < 4) {
    this.userResults = [];
    return;
  }

  this.isSearchingUsers = true;

  this.userService.searchUsers(term)
    .pipe(
      finalize(() => {
        this.isSearchingUsers = false;
        this.cdr.detectChanges();
      })
    )
    .subscribe({
      next: response => {
        this.userResults = response ?? [];
      },
      error: error => {
        console.error('Error searching users', error);
        this.userResults = [];
      }
    });
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
      return;
    }

    this.conversations.unshift({
      userId: this.selectedConversation.userId,
      displayName: this.selectedConversation.displayName,
      avatarUrl: this.selectedConversation.avatarUrl,
      lastMessage: message.text,
      lastMessageAtUTC: message.sentAtUTC,
      unreadCount: 0
    });
  }
}
import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize, forkJoin, Subscription } from 'rxjs';

import { MessageService } from '../../core/services/message.service';
import { RealtimeService } from '../../core/services/realtime.service';
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
  styleUrl: './messages.scss',
  styles: [`
    .avatar-wrap {
      position: relative;
      width: 50px;
      height: 50px;
      flex-shrink: 0;
    }

    .avatar-wrap img {
      width: 50px;
      height: 50px;
      border-radius: 50%;
      object-fit: cover;
      object-position: top center;
      display: block;
    }

    .chat-header .avatar-wrap,
    .chat-header .avatar-wrap img {
      width: 48px;
      height: 48px;
    }

    .user-result .avatar-wrap,
    .user-result .avatar-wrap img {
      width: 48px;
      height: 48px;
    }

    .presence-dot {
      position: absolute;
      right: 0;
      bottom: 2px;
      width: 12px;
      height: 12px;
      border-radius: 50%;
      background: #5b6b63;
      border: 2px solid #02120a;
    }

    .presence-dot.online {
      background: #00ff88;
      box-shadow: 0 0 8px rgba(0, 255, 136, .8);
    }

    .online-label {
      color: #00ff88 !important;
    }
  `]
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
  private pendingQueryUserId = '';

  constructor(
    private messageService: MessageService,
    private userService: UserService,
    private realtime: RealtimeService,
    private route: ActivatedRoute,
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

    this.subscription.add(
      this.route.queryParamMap.subscribe(params => {
        const userId = params.get('userId')?.trim();

        if (!userId) {
          return;
        }

        this.pendingQueryUserId = userId;
        this.openFromQuery(userId);
      })
    );

    this.subscription.add(
      this.realtime.messages$.subscribe(message => {
        this.handleLiveMessage(message);
      })
    );

    this.subscription.add(
      this.realtime.messageRead$.subscribe(event => {
        this.messages = this.messages.map(message =>
          message.messageId === event.messageId
            ? { ...message, isRead: true }
            : message
        );

        this.cdr.detectChanges();
      })
    );

    this.subscription.add(
      this.realtime.onlineUsers$.subscribe(() => {
        this.cdr.detectChanges();
      })
    );

    this.subscription.add(
      this.realtime.userOnline$.subscribe(() => {
        this.cdr.detectChanges();
      })
    );

    this.subscription.add(
      this.realtime.userOffline$.subscribe(() => {
        this.cdr.detectChanges();
      })
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

  isUserOnline(userId?: string | null): boolean {
    if (!userId) {
      return false;
    }

    return this.realtime.isOnline(userId);
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

          if (this.pendingQueryUserId) {
            this.openFromQuery(this.pendingQueryUserId);
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
          this.appendMessage(message);

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

  private openFromQuery(userId: string): void {
    if (!userId) {
      return;
    }

    if (this.selectedUserId === userId && this.selectedConversation) {
      this.pendingQueryUserId = '';
      return;
    }

    const existing = this.conversations.find(x => x.userId === userId);

    if (existing) {
      this.pendingQueryUserId = '';
      this.openConversation(existing);
      return;
    }

    this.selectedUserId = userId;
    this.selectedConversation = {
      userId,
      displayName: 'Conversation',
      avatarUrl: null,
      lastMessage: '',
      lastMessageAtUTC: new Date().toISOString(),
      unreadCount: 0
    };

    this.pendingQueryUserId = '';
    this.loadConversation(userId);
  }

  private handleLiveMessage(message: MessageDto): void {
    const otherUserId = message.isMine
      ? message.receiverUserId
      : message.senderUserId;

    if (this.selectedUserId && otherUserId === this.selectedUserId) {
      this.appendMessage(message);

      if (!message.isMine && !message.isRead) {
        this.messageService.markAsRead(message.messageId).subscribe({
          next: () => {
            this.messages = this.messages.map(item =>
              item.messageId === message.messageId
                ? { ...item, isRead: true }
                : item
            );
            this.cdr.detectChanges();
          },
          error: error => console.error('Error marking live message as read:', error)
        });
      }
    }

    this.upsertConversationFromLive(message, otherUserId);
    this.cdr.detectChanges();
  }

  private appendMessage(message: MessageDto): void {
    if (!message?.messageId) {
      return;
    }

    if (this.messages.some(x => x.messageId === message.messageId)) {
      return;
    }

    this.messages = [...this.messages, message];
  }

  private upsertConversationFromLive(message: MessageDto, otherUserId: string): void {
    const existing = this.conversations.find(x => x.userId === otherUserId);
    const isSelected = this.selectedUserId === otherUserId;

    if (existing) {
      existing.lastMessage = message.text;
      existing.lastMessageAtUTC = message.sentAtUTC;

      if (!message.isMine && !isSelected) {
        existing.unreadCount += 1;
      }

      if (isSelected) {
        existing.unreadCount = 0;
        this.selectedConversation = existing;
      }

      this.conversations = [
        existing,
        ...this.conversations.filter(x => x.userId !== otherUserId)
      ];
      return;
    }

    const displayName = message.isMine ? message.receiverName : message.senderName;

    this.conversations = [
      {
        userId: otherUserId,
        displayName,
        avatarUrl: null,
        lastMessage: message.text,
        lastMessageAtUTC: message.sentAtUTC,
        unreadCount: message.isMine || isSelected ? 0 : 1
      },
      ...this.conversations
    ];
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

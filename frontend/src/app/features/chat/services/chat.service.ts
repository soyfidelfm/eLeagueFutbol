import { Injectable, computed, inject, signal } from '@angular/core';
import { Subscription, firstValueFrom } from 'rxjs';

import { MessageService } from '../../../core/services/message.service';
import { RealtimeService } from '../../../core/services/realtime.service';
import { UserService } from '../../../core/services/user.service';
import {
  ConversationDto,
  MessageDto,
  SendMessageDto
} from '../../../core/models/message.model';

export const MAX_MESSAGE_LENGTH = 2000;
const PAGE_SIZE = 30;
const TYPING_THROTTLE_MS = 2500;
const TYPING_HIDE_MS = 3000;

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly messageService = inject(MessageService);
  private readonly realtime = inject(RealtimeService);
  private readonly userService = inject(UserService);

  readonly panelOpen = signal(false);
  readonly composing = signal(false);
  readonly activeId = signal<string | null>(null);

  readonly conversations = signal<ConversationDto[]>([]);
  readonly loadingConversations = signal(false);
  readonly conversationsLoaded = signal(false);
  readonly conversationsError = signal(false);

  readonly activeMessages = signal<MessageDto[]>([]);
  readonly activeHasMore = signal(false);
  readonly loadingThread = signal<string | null>(null);
  readonly threadError = signal<string | null>(null);
  readonly loadingOlder = signal<string | null>(null);

  readonly drafts = signal<Record<string, string>>({});
  readonly typing = signal<Record<string, { name: string } | null>>({});

  readonly me = signal<{ id: string; name: string }>({ id: '', name: '' });

  readonly totalUnread = computed(() =>
    this.conversations().reduce((sum, c) => sum + (c.unreadCount ?? 0), 0)
  );

  private sub = new Subscription();
  private typingTimers = new Map<string, ReturnType<typeof setTimeout>>();
  private lastTypingSent = 0;

  constructor() {
    this.sub.add(
      this.userService.currentUser$.subscribe(u => {
        if (u) this.me.set({ id: u.userId, name: u.displayName });
      })
    );

    this.sub.add(
      this.realtime.messages$.subscribe(msg => this.handleLiveMessage(msg))
    );

    this.sub.add(
      this.realtime.typing$.subscribe(ev => this.handleTyping(ev?.senderUserId))
    );

    this.sub.add(
      this.realtime.onlineUsers$.subscribe(() => {
        // presence is read live via isOnline()
      })
    );
  }

  openPanel(): void {
    this.panelOpen.set(true);
    if (!this.conversationsLoaded()) this.loadConversations();
  }

  closePanel(): void {
    this.panelOpen.set(false);
    this.activeId.set(null);
    this.composing.set(false);
  }

  startComposing(): void {
    this.composing.set(true);
    this.activeId.set(null);
  }

  closeThread(): void {
    this.activeId.set(null);
  }

  peerOf(conv: ConversationDto | null | undefined) {
    if (!conv) return null;
    return {
      id: conv.userId,
      name: conv.displayName,
      avatarUrl: conv.avatarUrl ?? null,
      online: this.realtime.isOnline(conv.userId)
    };
  }

  activeConversation = computed(() => {
    const id = this.activeId();
    return id ? this.conversations().find(c => c.userId === id) ?? null : null;
  });

  loadConversations(): void {
    this.loadingConversations.set(true);
    this.conversationsError.set(false);

    this.messageService.getConversations().subscribe({
      next: data => {
        this.conversations.set(data ?? []);
        this.conversationsLoaded.set(true);
        this.loadingConversations.set(false);
      },
      error: () => {
        this.conversationsError.set(true);
        this.loadingConversations.set(false);
      }
    });
  }

  openConversation(userId: string): void {
    this.activeId.set(userId);
    this.composing.set(false);
    this.loadThread(userId);

    // clear unread locally
    this.conversations.update(list =>
      list.map(c => c.userId === userId ? { ...c, unreadCount: 0 } : c)
    );
  }

  loadThread(userId: string): void {
    this.loadingThread.set(userId);
    this.threadError.set(null);

    this.messageService.getConversation(userId, null, PAGE_SIZE).subscribe({
      next: res => {
        if (this.activeId() !== userId) return;
        this.activeMessages.set(res?.items ?? []);
        this.activeHasMore.set(!!res?.hasMore);
        this.loadingThread.set(null);
        this.markVisibleAsRead(res?.items ?? []);
      },
      error: () => {
        this.threadError.set(userId);
        this.loadingThread.set(null);
      }
    });
  }

  loadOlder(userId: string): void {
    if (
      this.loadingOlder() === userId ||
      !this.activeHasMore() ||
      this.activeMessages().length === 0
    ) return;

    const oldest = this.activeMessages()[0]?.messageId;
    if (!oldest) return;

    this.loadingOlder.set(userId);

    this.messageService.getConversation(userId, oldest, PAGE_SIZE).subscribe({
      next: res => {
        if (this.activeId() !== userId) return;
        const known = new Set(this.activeMessages().map(m => m.messageId));
        const older = (res?.items ?? []).filter(m => !known.has(m.messageId));
        this.activeMessages.update(curr => [...older, ...curr]);
        this.activeHasMore.set(!!res?.hasMore && older.length > 0);
        this.loadingOlder.set(null);
      },
      error: () => this.loadingOlder.set(null)
    });
  }

  draftFor(id: string): string {
    return this.drafts()[id] ?? '';
  }

  setDraft(id: string, text: string): void {
    this.drafts.update(d => ({ ...d, [id]: text }));
  }

  notifyTyping(userId: string): void {
    const now = Date.now();
    if (now - this.lastTypingSent < TYPING_THROTTLE_MS) return;
    this.lastTypingSent = now;
    void this.realtime.sendTyping(userId);
  }

  send(userId: string, text: string): void {
    const trimmed = text.trim();
    if (!trimmed || trimmed.length > MAX_MESSAGE_LENGTH) return;

    const dto: SendMessageDto = { receiverUserId: userId, text: trimmed };

    this.messageService.sendMessage(dto).subscribe({
      next: msg => {
        this.appendMessage(msg);
        this.upsertConversation(msg, userId);
        this.setDraft(userId, '');
      },
      error: err => console.error('Send failed', err)
    });
  }

  private appendMessage(msg: MessageDto): void {
    if (this.activeMessages().some(m => m.messageId === msg.messageId)) return;
    this.activeMessages.update(list => [...list, msg]);
  }

  private handleLiveMessage(msg: MessageDto): void {
    const otherId = msg.isMine ? msg.receiverUserId : msg.senderUserId;

    if (this.activeId() === otherId) {
      this.appendMessage(msg);
      if (!msg.isMine) {
        this.clearTyping(otherId);
        if (!msg.isRead) this.messageService.markAsRead(msg.messageId).subscribe();
      }
    }

    this.upsertConversation(msg, otherId);

    if (!msg.isMine && this.activeId() !== otherId) {
      window.dispatchEvent(new CustomEvent('eleague-message-received'));
    }
  }

  private upsertConversation(msg: MessageDto, otherId: string): void {
    const existing = this.conversations().find(c => c.userId === otherId);
    const isActive = this.activeId() === otherId;

    if (existing) {
      const updated: ConversationDto = {
        ...existing,
        lastMessage: msg.text,
        lastMessageAtUTC: msg.sentAtUTC,
        unreadCount: !msg.isMine && !isActive ? (existing.unreadCount ?? 0) + 1 : 0
      };
      this.conversations.update(list => [
        updated,
        ...list.filter(c => c.userId !== otherId)
      ]);
    } else {
      const displayName = msg.isMine ? msg.receiverName : msg.senderName;
      this.conversations.update(list => [
        {
          userId: otherId,
          displayName,
          avatarUrl: null,
          lastMessage: msg.text,
          lastMessageAtUTC: msg.sentAtUTC,
          unreadCount: msg.isMine || isActive ? 0 : 1
        },
        ...list
      ]);
    }
  }

  private handleTyping(senderUserId?: string): void {
    if (!senderUserId) return;

    const conv = this.conversations().find(c => c.userId === senderUserId);
    const name = conv?.displayName ?? 'User';

    this.typing.update(t => ({ ...t, [senderUserId]: { name } }));

    const prev = this.typingTimers.get(senderUserId);
    if (prev) clearTimeout(prev);

    this.typingTimers.set(
      senderUserId,
      setTimeout(() => this.clearTyping(senderUserId), TYPING_HIDE_MS)
    );
  }

  private clearTyping(userId: string): void {
    this.typing.update(t => ({ ...t, [userId]: null }));
    const t = this.typingTimers.get(userId);
    if (t) clearTimeout(t);
    this.typingTimers.delete(userId);
  }

  private markVisibleAsRead(messages: MessageDto[]): void {
    const unread = messages.filter(m => !m.isMine && !m.isRead);
    for (const m of unread) {
      this.messageService.markAsRead(m.messageId).subscribe();
    }
    if (unread.length) {
      window.dispatchEvent(new CustomEvent('eleague-message-read'));
    }
  }

  isOnline(userId: string): boolean {
    return this.realtime.isOnline(userId);
  }
}

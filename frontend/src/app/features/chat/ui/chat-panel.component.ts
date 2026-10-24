import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { TranslationService } from '../../../core/i18n/translation.service';
import { shortTime } from '../chat-time';
import { ChatService } from '../services/chat.service';
import { ChatThreadComponent } from './chat-thread.component';
import { NewChatComponent } from './new-chat.component';

@Component({
  selector: 'app-chat-panel',
  standalone: true,
  imports: [TranslatePipe, ChatThreadComponent, NewChatComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './chat-panel.component.scss',
  templateUrl: './chat-panel.component.html',
})
export class ChatPanelComponent {
  readonly chat = inject(ChatService);
  private readonly i18n = inject(TranslationService);

  readonly filter = signal('');

  readonly rows = computed(() => {
    const me = this.chat.me().id;
    const typing = this.chat.typing();
    const term = this.filter().trim().toLowerCase();
    const locale = this.i18n.locale;

    return this.chat.conversations()
      .map(c => {
        const peer = this.chat.peerOf(c);
        const last = c.lastMessage;
        const preview = last || '';
        return {
          id: c.userId,
          peer,
          name: peer?.name ?? '',
          preview,
          time: c.lastMessageAtUTC ? shortTime(c.lastMessageAtUTC, locale) : '',
          unread: c.unreadCount ?? 0,
          typing: !!typing[c.userId],
          online: peer?.online ?? false,
        };
      })
      .filter(r => !term || r.name.toLowerCase().includes(term) || r.preview.toLowerCase().includes(term));
  });

  badge(n: number): string {
    return n > 99 ? '99+' : String(n);
  }

  close(): void {
    this.chat.closePanel();
  }
}

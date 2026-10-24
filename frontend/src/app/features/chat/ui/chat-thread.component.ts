import {
  ChangeDetectionStrategy, Component, ElementRef, Injector, afterNextRender, computed, effect, inject, output, untracked, viewChild,
} from '@angular/core';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { TranslationService } from '../../../core/i18n/translation.service';
import { MessageDto } from '../../../core/models/message.model';
import { clockTime, daysAgo, longDate } from '../chat-time';
import { ChatService, MAX_MESSAGE_LENGTH } from '../services/chat.service';

const GROUP_WINDOW_MS = 5 * 60_000;
const NEAR_BOTTOM_PX = 120;
const LOAD_OLDER_PX = 60;

interface DayItem { kind: 'day'; key: string; label: string }
interface GroupItem {
  kind: 'group'; key: string; mine: boolean; messages: MessageDto[]; lastAt: number; time: string;
}
type ThreadItem = DayItem | GroupItem;

@Component({
  selector: 'app-chat-thread',
  standalone: true,
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './chat-thread.component.scss',
  templateUrl: './chat-thread.component.html',
})
export class ChatThreadComponent {
  readonly chat = inject(ChatService);
  private readonly i18n = inject(TranslationService);
  private readonly injector = inject(Injector);
  readonly closed = output<void>();

  readonly scroller = viewChild<ElementRef<HTMLElement>>('scroller');
  readonly box = viewChild<ElementRef<HTMLTextAreaElement>>('box');
  readonly maxLength = MAX_MESSAGE_LENGTH;

  readonly conv = this.chat.activeConversation;
  readonly peer = computed(() => this.chat.peerOf(this.conv()));
  readonly typing = computed(() => {
    const id = this.chat.activeId();
    return id ? this.chat.typing()[id] ?? null : null;
  });
  readonly loading = computed(() => this.chat.loadingThread() === this.chat.activeId());
  readonly failedLoad = computed(() => this.chat.threadError() === this.chat.activeId());
  readonly loadingOlder = computed(() => this.chat.loadingOlder() === this.chat.activeId());

  readonly items = computed<ThreadItem[]>(() => {
    const locale = this.i18n.locale;
    const out: ThreadItem[] = [];
    let day = '';
    let group: GroupItem | null = null;

    for (const m of this.chat.activeMessages()) {
      const d = new Date(m.sentAtUTC);
      const dayKey = d.toDateString();
      if (dayKey !== day) {
        day = dayKey;
        group = null;
        out.push({ kind: 'day', key: `d-${dayKey}`, label: this.dayLabel(m.sentAtUTC, locale) });
      }
      const at = d.getTime();
      if (group && group.mine === m.isMine && at - group.lastAt < GROUP_WINDOW_MS) {
        group.messages.push(m);
        group.lastAt = at;
        group.time = clockTime(m.sentAtUTC, locale);
      } else {
        group = {
          kind: 'group', key: `g-${m.messageId}`, mine: m.isMine,
          messages: [m], lastAt: at, time: clockTime(m.sentAtUTC, locale),
        };
        out.push(group);
      }
    }
    return out;
  });

  private lastId: string | null = null;
  private lastFirst: string | undefined;
  private lastCount = 0;

  constructor() {
    effect(() => {
      const msgs = this.chat.activeMessages();
      const id = this.chat.activeId();
      this.typing();
      untracked(() => this.onMessagesChanged(id, msgs));
    });

    afterNextRender(() => this.focusInput(), { injector: this.injector });
  }

  private onMessagesChanged(id: string | null, msgs: MessageDto[]): void {
    const el = this.scroller()?.nativeElement;
    const first = msgs[0]?.messageId;
    const switched = id !== this.lastId;
    const prepended = !switched && !!this.lastFirst && first !== this.lastFirst && msgs.length > this.lastCount;
    const lastMine = msgs.length > this.lastCount && msgs[msgs.length - 1]?.isMine;
    const prevHeight = el?.scrollHeight ?? 0;
    const prevTop = el?.scrollTop ?? 0;
    const nearBottom = !el || el.scrollHeight - el.scrollTop - el.clientHeight < NEAR_BOTTOM_PX;
    this.lastId = id;
    this.lastFirst = first;
    this.lastCount = msgs.length;

    afterNextRender(() => {
      const box = this.scroller()?.nativeElement;
      if (!box) return;
      if (prepended) box.scrollTop = box.scrollHeight - prevHeight + prevTop;
      else if (switched || nearBottom || lastMine) this.scrollToBottom(box, !switched);
    }, { injector: this.injector });
  }

  private scrollToBottom(el: HTMLElement, smooth: boolean): void {
    const reduce = typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (typeof el.scrollTo === 'function') el.scrollTo({ top: el.scrollHeight, behavior: smooth && !reduce ? 'smooth' : 'auto' });
    else el.scrollTop = el.scrollHeight;
  }

  onScroll(): void {
    const el = this.scroller()?.nativeElement;
    const id = this.chat.activeId();
    if (el && id && el.scrollTop < LOAD_OLDER_PX && this.chat.activeHasMore()) this.chat.loadOlder(id);
  }

  draft(): string {
    const id = this.chat.activeId();
    return id ? this.chat.draftFor(id) : '';
  }

  onInput(e: Event): void {
    const ta = e.target as HTMLTextAreaElement;
    const id = this.chat.activeId();
    if (!id) return;
    this.chat.setDraft(id, ta.value);
    this.autoGrow(ta);
    if (ta.value.trim()) this.chat.notifyTyping(id);
  }

  onEnter(e: Event): void {
    const ke = e as KeyboardEvent;
    if (ke.shiftKey || ke.isComposing) return;
    e.preventDefault();
    this.send();
  }

  send(): void {
    const id = this.chat.activeId();
    const ta = this.box()?.nativeElement;
    if (!id || !ta || !ta.value.trim()) return;
    this.chat.send(id, ta.value);
    ta.value = '';
    this.autoGrow(ta);
    ta.focus();
  }

  bubble(mine: boolean, first: boolean, last: boolean): string {
    const base = mine
      ? 'bg-[#00ff88] text-[#02120a]'
      : 'bg-[#111a14] text-white border border-[rgba(255,255,255,0.08)]';
    const corners = mine
      ? `${first ? '' : 'rounded-tr-md'} ${last ? '' : 'rounded-br-md'}`
      : `${first ? '' : 'rounded-tl-md'} ${last ? '' : 'rounded-bl-md'}`;
    return `${base} ${corners}`;
  }

  private dayLabel(iso: string, locale: string): string {
    const n = daysAgo(iso);
    if (n === 0) return this.i18n.t('Today');
    if (n === 1) return this.i18n.t('Yesterday');
    return longDate(iso, locale);
  }

  private autoGrow(ta: HTMLTextAreaElement): void {
    ta.style.height = 'auto';
    ta.style.height = `${Math.min(ta.scrollHeight, 112)}px`;
  }

  private focusInput(): void {
    const coarse = typeof matchMedia === 'function' && matchMedia('(pointer: coarse)').matches;
    if (!coarse) this.box()?.nativeElement.focus();
  }
}

import { ChangeDetectionStrategy, Component, inject, output, signal } from '@angular/core';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { UserService, UserSearchResult } from '../../../core/services/user.service';
import { ChatService } from '../services/chat.service';

@Component({
  selector: 'app-new-chat',
  standalone: true,
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex min-h-0 flex-1 flex-col' },
  template: `
    <header class="flex items-center gap-2 border-b border-[rgba(0,255,136,0.18)] px-2 py-2.5">
      <button type="button" (click)="chat.composing.set(false)" aria-label="Back"
              class="rounded-lg p-1.5 text-[#8d97a5] hover:bg-[#111a14] hover:text-white focus:outline-none focus-visible:ring-2 focus-visible:ring-[#00ff88]">
        <svg class="h-5 w-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24" aria-hidden="true"><path d="M15 18l-6-6 6-6"/></svg>
      </button>
      <h2 class="flex-1 text-sm font-semibold text-white">{{ 'New Message' | t }}</h2>
      <button type="button" (click)="closed.emit()" aria-label="Close"
              class="rounded-lg p-1.5 text-[#8d97a5] hover:bg-[#111a14] hover:text-white focus:outline-none focus-visible:ring-2 focus-visible:ring-[#00ff88]">
        <svg class="h-5 w-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24" aria-hidden="true"><path d="M6 6l12 12M18 6L6 18"/></svg>
      </button>
    </header>

    <div class="p-3">
      <input type="search" autocomplete="off" [value]="term()" (input)="onSearch($event)"
             placeholder="Search player..."
             class="w-full rounded-full border border-[rgba(0,255,136,0.28)] bg-[#111a14] px-4 py-2 text-sm text-white placeholder:text-[#5f6977] focus:border-[#00ff88] focus:outline-none" />
    </div>

    <div class="flex-1 overflow-y-auto px-2 pb-2">
      @if (searching()) {
        <div class="flex justify-center py-6"><span class="spinner h-5 w-5 rounded-full border-2 border-[rgba(0,255,136,0.28)] border-t-[#00ff88]"></span></div>
      } @else if (term().trim().length < 2) {
        <p class="px-4 py-8 text-center text-sm text-[#8d97a5]">Type at least 2 characters.</p>
      } @else if (!results().length) {
        <p class="px-4 py-8 text-center text-sm text-[#8d97a5]">No players found.</p>
      } @else {
        <ul>
          @for (u of results(); track u.userId) {
            <li>
              <button type="button" (click)="startWith(u)"
                      class="flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-left hover:bg-[#111a14]">
                @if (u.avatarUrl) {
                  <img [src]="u.avatarUrl" [alt]="u.displayName" class="h-10 w-10 rounded-full object-cover object-top border border-[rgba(0,255,136,0.35)]" />
                } @else {
                  <span class="flex h-10 w-10 items-center justify-center rounded-full bg-[#111a14] border border-[rgba(0,255,136,0.35)] text-sm font-bold text-[#00ff88]">{{ u.displayName.charAt(0) }}</span>
                }
                <span class="min-w-0 flex-1">
                  <span class="block truncate text-sm font-medium text-white">{{ u.displayName }}</span>
                  <span class="block truncate text-xs text-[#8d97a5]">{{ chat.isOnline(u.userId) ? 'Online' : 'Start private message' }}</span>
                </span>
              </button>
            </li>
          }
        </ul>
      }
    </div>
  `,
  styles: [`
    .spinner { animation: spin 0.8s linear infinite; }
    @keyframes spin { to { transform: rotate(360deg); } }
  `]
})
export class NewChatComponent {
  readonly chat = inject(ChatService);
  private readonly userService = inject(UserService);
  readonly closed = output<void>();

  readonly term = signal('');
  readonly results = signal<UserSearchResult[]>([]);
  readonly searching = signal(false);
  private timer?: ReturnType<typeof setTimeout>;

  onSearch(e: Event): void {
    const value = (e.target as HTMLInputElement).value;
    this.term.set(value);
    if (this.timer) clearTimeout(this.timer);

    const q = value.trim();
    if (q.length < 2) {
      this.results.set([]);
      return;
    }

    this.timer = setTimeout(() => {
      this.searching.set(true);
      this.userService.searchUsers(q).subscribe({
        next: list => {
          this.results.set(list ?? []);
          this.searching.set(false);
        },
        error: () => {
          this.results.set([]);
          this.searching.set(false);
        }
      });
    }, 300);
  }

  startWith(user: UserSearchResult): void {
    const existing = this.chat.conversations().find(c => c.userId === user.userId);
    if (!existing) {
      this.chat.conversations.update(list => [
        {
          userId: user.userId,
          displayName: user.displayName,
          avatarUrl: user.avatarUrl,
          lastMessage: '',
          lastMessageAtUTC: new Date().toISOString(),
          unreadCount: 0
        },
        ...list
      ]);
    }
    this.chat.openConversation(user.userId);
  }
}

import {
  ChangeDetectorRef,
  Component,
  ElementRef,
  HostListener,
  OnDestroy,
  OnInit,
  ViewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  Subject,
  Subscription,
  catchError,
  debounceTime,
  distinctUntilChanged,
  map,
  of,
  switchMap,
  tap
} from 'rxjs';

import { SearchService } from '../../core/services/search.service';
import {
  GlobalSearchResult,
  SearchClubResult,
  SearchMatchResult,
  SearchPlayerResult
} from '../../core/models/search.model';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LocalizedDatePipe } from '../../core/i18n/localized-date.pipe';
import { EmptyState } from '../../shared/components/empty-state/empty-state';

type ResultItem =
  | { kind: 'player'; item: SearchPlayerResult }
  | { kind: 'club'; item: SearchClubResult }
  | { kind: 'match'; item: SearchMatchResult };

const MIN_CHARS = 2;
const EMPTY: GlobalSearchResult = { players: [], clubs: [], matches: [] };

/**
 * Navbar global search: magnifier button that opens a modal panel.
 * Ctrl/Cmd+K or "/" opens it, Esc closes, ↑/↓ + Enter navigate results.
 */
@Component({
  selector: 'app-global-search',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe, LocalizedDatePipe, EmptyState],
  templateUrl: './global-search.html',
  styleUrl: './global-search.scss'
})
export class GlobalSearch implements OnInit, OnDestroy {
  @ViewChild('searchInput') searchInput?: ElementRef<HTMLInputElement>;

  isOpen = false;
  query = '';
  isLoading = false;
  hasError = false;
  searched = false;
  results: GlobalSearchResult = EMPTY;
  activeIndex = -1;

  private readonly query$ = new Subject<string>();
  private subscription = new Subscription();

  constructor(
    private readonly searchService: SearchService,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.subscription.add(
      this.query$
        .pipe(
          map(q => q.trim()),
          debounceTime(300),
          distinctUntilChanged(),
          tap(q => {
            this.hasError = false;
            this.isLoading = q.length >= MIN_CHARS;
            if (q.length < MIN_CHARS) {
              this.results = EMPTY;
              this.searched = false;
            }
            this.cdr.detectChanges();
          }),
          switchMap(q =>
            q.length < MIN_CHARS
              ? of(null)
              : this.searchService.search(q).pipe(
                  catchError(() => {
                    this.hasError = true;
                    return of(EMPTY);
                  })
                )
          )
        )
        .subscribe(result => {
          this.isLoading = false;
          if (result) {
            this.results = {
              players: result.players ?? [],
              clubs: result.clubs ?? [],
              matches: result.matches ?? []
            };
            this.searched = true;
          }
          this.activeIndex = this.flatResults.length ? 0 : -1;
          this.cdr.detectChanges();
        })
    );
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
    document.body.classList.remove('global-search-open');
  }

  get tooShort(): boolean {
    return this.query.trim().length < MIN_CHARS;
  }

  get flatResults(): ResultItem[] {
    return [
      ...this.results.players.map(item => ({ kind: 'player' as const, item })),
      ...this.results.clubs.map(item => ({ kind: 'club' as const, item })),
      ...this.results.matches.map(item => ({ kind: 'match' as const, item }))
    ];
  }

  get hasResults(): boolean {
    return this.flatResults.length > 0;
  }

  /** Index of an item inside flatResults, for keyboard highlight. */
  indexOf(kind: ResultItem['kind'], i: number): number {
    if (kind === 'player') return i;
    if (kind === 'club') return this.results.players.length + i;
    return this.results.players.length + this.results.clubs.length + i;
  }

  open(): void {
    if (this.isOpen) return;
    this.isOpen = true;
    document.body.classList.add('global-search-open');
    this.cdr.detectChanges();
    setTimeout(() => this.searchInput?.nativeElement.focus());
  }

  close(): void {
    if (!this.isOpen) return;
    this.isOpen = false;
    document.body.classList.remove('global-search-open');
    this.cdr.detectChanges();
  }

  clear(): void {
    this.query = '';
    this.onQueryChange('');
    this.searchInput?.nativeElement.focus();
  }

  onQueryChange(value: string): void {
    this.query$.next(value ?? '');
  }

  @HostListener('document:keydown', ['$event'])
  onDocumentKeydown(event: KeyboardEvent): void {
    const key = event.key;

    if ((event.ctrlKey || event.metaKey) && key.toLowerCase() === 'k') {
      event.preventDefault();
      this.isOpen ? this.close() : this.open();
      return;
    }

    if (!this.isOpen) {
      const target = event.target as HTMLElement | null;
      const typing =
        !!target &&
        (target.tagName === 'INPUT' ||
          target.tagName === 'TEXTAREA' ||
          target.tagName === 'SELECT' ||
          target.isContentEditable);

      if (key === '/' && !typing) {
        event.preventDefault();
        this.open();
      }
      return;
    }

    if (key === 'Escape') {
      event.preventDefault();
      this.close();
      return;
    }

    const total = this.flatResults.length;
    if (!total) return;

    if (key === 'ArrowDown') {
      event.preventDefault();
      this.activeIndex = (this.activeIndex + 1) % total;
      this.scrollActiveIntoView();
    } else if (key === 'ArrowUp') {
      event.preventDefault();
      this.activeIndex = (this.activeIndex - 1 + total) % total;
      this.scrollActiveIntoView();
    } else if (key === 'Enter' && this.activeIndex >= 0) {
      event.preventDefault();
      this.select(this.flatResults[this.activeIndex]);
    }
  }

  select(result: ResultItem): void {
    this.close();

    switch (result.kind) {
      case 'player':
        // Only the own profile has a page; other users open a conversation.
        if (result.item.isMe) {
          void this.router.navigate(['/profile']);
        } else {
          void this.router.navigate(['/messages'], {
            queryParams: { userId: result.item.userId }
          });
        }
        break;

      case 'club':
        // /clubs only shows the own squad; other clubs are focused in Rankings.
        if (result.item.isMine) {
          void this.router.navigate(['/clubs']);
        } else {
          void this.router.navigate(['/rankings'], {
            queryParams: { seasonId: result.item.seasonId, clubId: result.item.clubId }
          });
        }
        break;

      case 'match':
        void this.router.navigate(['/matches'], {
          queryParams: {
            seasonId: result.item.seasonId,
            week: result.item.weekNumber,
            matchId: result.item.matchId
          }
        });
        break;
    }
  }

  initial(value?: string | null): string {
    return (value ?? '').trim().charAt(0).toUpperCase() || '?';
  }

  isFinished(match: SearchMatchResult): boolean {
    return (match.status ?? '').toLowerCase() === 'finished';
  }

  onImgError(event: Event): void {
    (event.target as HTMLImageElement).style.display = 'none';
  }

  private scrollActiveIntoView(): void {
    this.cdr.detectChanges();
    const el = document.querySelector('.gs-item.active');
    el?.scrollIntoView({ block: 'nearest' });
  }
}

import { tap } from 'rxjs';
import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subscription, finalize, take } from 'rxjs';

import { TransferService } from '../../core/services/transfer.service';
import { SeasonService } from '../../core/services/season.service';
import { RealtimeService } from '../../core/services/realtime.service';
import { AppLoading } from '../../shared/components/app-loading/app-loading';
import { TransferBoard, TransferMarketPlayer, TransferOffer } from '../../core/models/transfer.model';
import { ConfirmDialogService } from '../../shared/components/confirm-dialog/confirm-dialog.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { NeonDropdownComponent, NeonDropdownOption } from '../../shared/components/neon-dropdown/neon-dropdown';

type MarketSort = 'price-desc' | 'price-asc' | 'rating' | 'name';

@Component({
  selector: 'app-transfers',
  standalone: true,
  imports: [CommonModule, FormsModule, AppLoading,
    TranslatePipe, EmptyState, NeonDropdownComponent
  ],
  templateUrl: './transfers.html',
  styleUrl: './transfers.scss'
})
export class Transfers implements OnInit, OnDestroy {
  isLoading = true;
  isRefreshing = false;
  errorMessage = '';
  liveMessage = '';
  searchTerm = '';
  offerAmount: Record<string, number> = {};
  board: TransferBoard | null = null;
  seasonId = '';
  seasonClosed = false;

  // Market filters / sort (client-side: the board already ships the full market for one season).
  positionFilter = 'all';
  clubFilter = 'all';
  statusFilter = 'all';
  minPrice: number | null = null;
  maxPrice: number | null = null;
  sortBy: MarketSort = 'price-desc';

  readonly statusOptions: NeonDropdownOption[] = [
    { label: 'All statuses', value: 'all' },
    { label: 'Available', value: 'available' },
    { label: 'Offer sent', value: 'pending' }
  ];

  readonly sortOptions: NeonDropdownOption[] = [
    { label: 'Price: high to low', value: 'price-desc' },
    { label: 'Price: low to high', value: 'price-asc' },
    { label: 'Rating', value: 'rating' },
    { label: 'Name (A–Z)', value: 'name' }
  ];

  private subscription = new Subscription();

  constructor(
    private confirmDialog: ConfirmDialogService,
    private transfers: TransferService,
    private seasons: SeasonService,
    private realtime: RealtimeService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.seasons.getCurrent().pipe(take(1)).subscribe({
      next: current => {
        if (!current?.seasonId) {
          this.isLoading = false;
          this.errorMessage = 'No active season found.';
          this.cdr.detectChanges();
          return;
        }

        this.seasonId = current.seasonId;
        this.seasonClosed = current.phase === 'Closed' || !current.isActive;
        void this.realtime.joinSeason(this.seasonId);
        this.loadBoard(true);
      },
      error: () => {
        this.seasons.getAll().pipe(take(1)).subscribe({
          next: list => {
            const current = (list ?? []).find(x => x.isCurrent && x.isActive);
            if (!current?.seasonId) {
              this.isLoading = false;
              this.errorMessage = 'No active season found.';
              this.cdr.detectChanges();
              return;
            }

            this.seasonId = current.seasonId;
            this.seasonClosed = !current.isActive;
            void this.realtime.joinSeason(this.seasonId);
            this.loadBoard(true);
          },
          error: () => {
            this.isLoading = false;
            this.errorMessage = 'Could not load season.';
            this.cdr.detectChanges();
          }
        });
      }
    });

    this.subscription.add(
      this.realtime.draftEvents$.subscribe(event => {
        if (!this.isTransferEvent(event?.eventType)) {
          return;
        }

        if (event.seasonId && this.seasonId && event.seasonId !== this.seasonId) {
          return;
        }

        this.liveMessage = this.liveLabel(event.eventType);
        this.loadBoard(false);
      })
    );
  }

  ngOnDestroy(): void {
    if (this.seasonId) {
      void this.realtime.leaveSeason(this.seasonId);
    }

    this.subscription.unsubscribe();
  }

  get positionOptions(): NeonDropdownOption[] {
    const positions = [...new Set((this.board?.market ?? []).map(p => p.position).filter(Boolean))].sort();
    return [{ label: 'All positions', value: 'all' }, ...positions.map(p => ({ label: p, value: p }))];
  }

  get clubOptions(): NeonDropdownOption[] {
    const clubs = new Map<string, string>();
    for (const p of this.board?.market ?? []) {
      if (p.ownerUserClubId) clubs.set(p.ownerUserClubId, p.ownerClubName);
    }
    const sorted = [...clubs.entries()].sort((a, b) => a[1].localeCompare(b[1]));
    return [{ label: 'All clubs', value: 'all' }, ...sorted.map(([value, label]) => ({ label, value }))];
  }

  get hasActiveFilters(): boolean {
    return !!this.searchTerm.trim()
      || this.positionFilter !== 'all'
      || this.clubFilter !== 'all'
      || this.statusFilter !== 'all'
      || this.minPrice != null
      || this.maxPrice != null;
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.positionFilter = 'all';
    this.clubFilter = 'all';
    this.statusFilter = 'all';
    this.minPrice = null;
    this.maxPrice = null;
  }

  onSortChange(value: string): void {
    this.sortBy = value as MarketSort;
  }

  get filteredMarket(): TransferMarketPlayer[] {
    const term = this.searchTerm.trim().toLowerCase();
    const min = this.minPrice != null && `${this.minPrice}` !== '' ? Number(this.minPrice) : null;
    const max = this.maxPrice != null && `${this.maxPrice}` !== '' ? Number(this.maxPrice) : null;

    const list = (this.board?.market ?? []).filter(p => {
      if (term && !(
        p.name.toLowerCase().includes(term) ||
        p.ownerClubName.toLowerCase().includes(term) ||
        p.position.toLowerCase().includes(term)
      )) return false;
      if (this.positionFilter !== 'all' && p.position !== this.positionFilter) return false;
      if (this.clubFilter !== 'all' && p.ownerUserClubId !== this.clubFilter) return false;
      if (this.statusFilter === 'available' && p.hasMyPendingOffer) return false;
      if (this.statusFilter === 'pending' && !p.hasMyPendingOffer) return false;
      const price = p.marketValue ?? 0;
      if (min != null && !isNaN(min) && price < min) return false;
      if (max != null && !isNaN(max) && price > max) return false;
      return true;
    });

    const byName = (a: TransferMarketPlayer, b: TransferMarketPlayer) => a.name.localeCompare(b.name);
    switch (this.sortBy) {
      case 'price-asc':
        return list.sort((a, b) => (a.marketValue ?? 0) - (b.marketValue ?? 0) || byName(a, b));
      case 'rating':
        return list.sort((a, b) => b.overall - a.overall || byName(a, b));
      case 'name':
        return list.sort(byName);
      default:
        return list.sort((a, b) => (b.marketValue ?? 0) - (a.marketValue ?? 0) || byName(a, b));
    }
  }

  loadBoard(showLoader: boolean): void {
    if (!this.seasonId) {
      return;
    }

    if (showLoader) {
      this.isLoading = true;
    } else {
      this.isRefreshing = true;
    }

    this.transfers.getBoard(this.seasonId)
      .pipe(finalize(() => {
        this.isLoading = false;
        this.isRefreshing = false;
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: board => {
          this.board = board;
          this.errorMessage = '';
        },
        error: err => {
          this.errorMessage = err?.error?.message || 'Could not load transfer market.';
        }
      });
  }

  makeOffer(player: TransferMarketPlayer): void {
    if (this.seasonClosed) {
      this.errorMessage = 'This season is closed.';
      return;
    }

    const fee = Number(this.offerAmount[player.gamePlayerId] ?? player.marketValue ?? 0);
    if (!fee || fee <= 0) {
      this.errorMessage = 'Enter a valid offer amount.';
      return;
    }

    this.transfers.createOffer(this.seasonId, player.gamePlayerId, fee).subscribe({
      next: () => this.loadBoard(false),
      error: err => {
        this.errorMessage = err?.error?.message || 'Could not send offer.';
        this.cdr.detectChanges();
      }
    });
  }

  accept(offer: TransferOffer): void {
    this.transfers.acceptOffer(offer.transferRecordId).subscribe({
      next: () => this.loadBoard(false),
      error: err => {
        this.errorMessage = err?.error?.message || 'Could not accept offer.';
        this.cdr.detectChanges();
      }
    });
  }

  reject(offer: TransferOffer): void {
    void this.confirmDialog.confirm({
      title: 'Reject offer for {player}?',
      description: '{club} will be notified that you rejected the offer.',
      params: { player: offer.playerName, club: offer.toClubName },
      confirmLabel: 'Reject offer',
      cancelLabel: 'Keep it',
      action: () => this.transfers.rejectOffer(offer.transferRecordId).pipe(
        tap({
          next: () => this.loadBoard(false),
          error: err => {
            this.errorMessage = err?.error?.message || 'Could not reject offer.';
            this.cdr.detectChanges();
          }
        })
      )
    });
  }

  cancel(offer: TransferOffer): void {
    void this.confirmDialog.confirm({
      title: 'Cancel your offer for {player}?',
      description: 'The offer will be withdrawn. You can make a new one later.',
      params: { player: offer.playerName, club: offer.fromClubName },
      confirmLabel: 'Cancel offer',
      cancelLabel: 'Keep it',
      action: () => this.transfers.cancelOffer(offer.transferRecordId).pipe(
        tap({
          next: () => this.loadBoard(false),
          error: err => {
            this.errorMessage = err?.error?.message || 'Could not cancel offer.';
            this.cdr.detectChanges();
          }
        })
      )
    });
  }

  private isTransferEvent(eventType?: string | null): boolean {
    const type = (eventType ?? '').toLowerCase();
    return type.startsWith('transfer');
  }

  private liveLabel(eventType: string): string {
    const type = (eventType ?? '').toLowerCase();

    if (type === 'transferoffer') {
      return 'New offer on the market';
    }

    if (type === 'transfercompleted' || type === 'transferaccepted') {
      return 'Transfer completed';
    }

    if (type === 'transferrejected') {
      return 'Offer rejected';
    }

    if (type === 'transfercancelled') {
      return 'Offer cancelled';
    }

    return 'Transfer market updated';
  }
}

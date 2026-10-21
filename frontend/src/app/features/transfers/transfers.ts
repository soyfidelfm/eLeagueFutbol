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

@Component({
  selector: 'app-transfers',
  standalone: true,
  imports: [CommonModule, FormsModule, AppLoading,
    TranslatePipe, EmptyState
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

  get filteredMarket(): TransferMarketPlayer[] {
    const term = this.searchTerm.trim().toLowerCase();
    const market = this.board?.market ?? [];
    if (!term) {
      return market;
    }
    return market.filter(p =>
      p.name.toLowerCase().includes(term) ||
      p.ownerClubName.toLowerCase().includes(term) ||
      p.position.toLowerCase().includes(term)
    );
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

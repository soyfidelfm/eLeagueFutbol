import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize, take } from 'rxjs';

import { TransferService } from '../../core/services/transfer.service';
import { SeasonService } from '../../core/services/season.service';
import { AppLoading } from '../../shared/components/app-loading/app-loading';
import { TransferBoard, TransferMarketPlayer, TransferOffer } from '../../core/models/transfer.model';

@Component({
  selector: 'app-transfers',
  standalone: true,
  imports: [CommonModule, FormsModule, AppLoading],
  templateUrl: './transfers.html',
  styleUrl: './transfers.scss'
})
export class Transfers implements OnInit {
  isLoading = true;
  errorMessage = '';
  searchTerm = '';
  offerAmount: Record<string, number> = {};
  board: TransferBoard | null = null;
  seasonId = '';

  constructor(
    private transfers: TransferService,
    private seasons: SeasonService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
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
        this.loadBoard();
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Could not load season.';
        this.cdr.detectChanges();
      }
    });
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

  loadBoard(): void {
    if (!this.seasonId) {
      return;
    }
    this.isLoading = true;
    this.transfers.getBoard(this.seasonId)
      .pipe(finalize(() => {
        this.isLoading = false;
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
    const fee = Number(this.offerAmount[player.gamePlayerId] ?? player.marketValue ?? 0);
    if (!fee || fee <= 0) {
      this.errorMessage = 'Enter a valid offer amount.';
      return;
    }
    this.transfers.createOffer(this.seasonId, player.gamePlayerId, fee).subscribe({
      next: () => this.loadBoard(),
      error: err => {
        this.errorMessage = err?.error?.message || 'Could not send offer.';
        this.cdr.detectChanges();
      }
    });
  }

  accept(offer: TransferOffer): void {
    this.transfers.acceptOffer(offer.transferRecordId).subscribe({
      next: () => this.loadBoard(),
      error: err => {
        this.errorMessage = err?.error?.message || 'Could not accept offer.';
        this.cdr.detectChanges();
      }
    });
  }

  reject(offer: TransferOffer): void {
    this.transfers.rejectOffer(offer.transferRecordId).subscribe({
      next: () => this.loadBoard(),
      error: err => {
        this.errorMessage = err?.error?.message || 'Could not reject offer.';
        this.cdr.detectChanges();
      }
    });
  }

  cancel(offer: TransferOffer): void {
    this.transfers.cancelOffer(offer.transferRecordId).subscribe({
      next: () => this.loadBoard(),
      error: err => {
        this.errorMessage = err?.error?.message || 'Could not cancel offer.';
        this.cdr.detectChanges();
      }
    });
  }
}

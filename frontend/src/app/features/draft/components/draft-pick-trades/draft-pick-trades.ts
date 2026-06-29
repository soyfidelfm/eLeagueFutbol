import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';

import { DraftService } from '../../../../core/services/draft.service';
import { CustomSelect, SelectOption } from '../../../../shared/components/custom-select/custom-select';

import {
  DraftCenter,
  DraftOrder,
  DraftPickAsset,
  DraftPickTrade
} from '../../../../core/models/draft.model';

interface TradeTargetClub {
  userClubId: string;
  clubName: string;
}

@Component({
  selector: 'app-draft-pick-trades',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CustomSelect
  ],
  templateUrl: './draft-pick-trades.html',
  styleUrl: './draft-pick-trades.scss'
})
export class DraftPickTrades implements OnChanges {

  @Input() seasonId = '';
  @Input() draftCenter: DraftCenter | null = null;
  @Input() incomingTrade: DraftPickTrade | null = null;
  @Input() refreshKey = 0;
  @Input() isLotteryRunning = false;

  @Output() refreshRequested = new EventEmitter<void>();
  @Output() toast = new EventEmitter<string>();
  @Output() incomingTradeCleared = new EventEmitter<void>();

  tradablePicks: DraftPickAsset[] = [];

  selectedPickIds: string[] = [];
  selectedTradeClubId: string | number | null = null;
  tradeMessage = '';

  showTradeModal = false;

  isLoadingTradablePicks = false;
  isSubmittingTrade = false;
  isRespondingTrade = false;

  constructor(
    private draftService: DraftService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['seasonId'] || changes['refreshKey']) {
      this.loadTradablePicks(true);
    }
  }

  get tradeTargetClubs(): TradeTargetClub[] {
    const draftCenter = this.draftCenter;

    if (!draftCenter) {
      return [];
    }

    return (draftCenter.draftOrder as DraftOrder[])
  .filter(team => team.userId !== draftCenter.myUserId)
  .map(team => ({
    userClubId: team.userClubId,
    clubName: team.clubName
  }))
  .filter(team => !!team.userClubId);
  }

  get tradeClubOptions(): SelectOption[] {
    return this.tradeTargetClubs.map(club => ({
      value: club.userClubId,
      label: club.clubName
    }));
  }

  get hasIncomingTrade(): boolean {
    return this.incomingTrade !== null;
  }

  get hasTradablePicks(): boolean {
    return this.tradablePicks.length > 0;
  }

  get canSubmitTrade(): boolean {
    return (
      !!this.selectedTradeClubId &&
      this.selectedPickIds.length > 0 &&
      !this.isSubmittingTrade
    );
  }

  get canRespondTrade(): boolean {
    return (
      !!this.incomingTrade &&
      !this.isRespondingTrade
    );
  }

  trackByPick(_: number, pick: DraftPickAsset): string {
    return pick.draftPickAssetId;
  }

  private resetTradeForm(): void {
    this.selectedPickIds = [];
    this.selectedTradeClubId = null;
    this.tradeMessage = '';
  }

  openTradeModal(): void {
    if (!this.seasonId) {
      this.toast.emit('Season was not found.');
      return;
    }

    if (!this.draftCenter?.draftOrder?.length) {
      this.toast.emit('Draft order is not ready yet.');
      return;
    }

    this.showTradeModal = true;
    this.resetTradeForm();
    this.loadTradablePicks(false);
    this.cdr.detectChanges();
  }

  closeTradeModal(): void {
    if (this.isSubmittingTrade) {
      return;
    }

    this.showTradeModal = false;
    this.resetTradeForm();
    this.cdr.detectChanges();
  }

  loadTradablePicks(silent = false): void {
    if (!this.seasonId) {
      return;
    }

    if (!silent) {
      this.isLoadingTradablePicks = true;
      this.cdr.detectChanges();
    }

    this.draftService
      .getTradablePicks(this.seasonId)
      .pipe(
        finalize(() => {
          if (!silent) {
            this.isLoadingTradablePicks = false;
          }

          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: picks => {
          this.tradablePicks = picks ?? [];

          this.selectedPickIds =
            this.selectedPickIds.filter(id =>
              this.tradablePicks.some(
                pick => pick.draftPickAssetId === id
              )
            );
        },
        error: err => {
          console.error('Error loading tradable picks:', err);

          if (!silent) {
            this.toast.emit(
              err?.error?.message ??
              'Could not load tradable picks.'
            );
          }
        }
      });
  }

  isPickSelected(draftPickAssetId: string): boolean {
    return this.selectedPickIds.includes(draftPickAssetId);
  }

  togglePickSelection(draftPickAssetId: string): void {
    if (this.isPickSelected(draftPickAssetId)) {
      this.selectedPickIds =
        this.selectedPickIds.filter(id => id !== draftPickAssetId);

      return;
    }

    this.selectedPickIds = [
      ...this.selectedPickIds,
      draftPickAssetId
    ];
  }

  sendTradeOffer(): void {
    if (!this.seasonId) {
      this.toast.emit('Season was not found.');
      return;
    }

    if (!this.selectedTradeClubId) {
      this.toast.emit('Select a club to trade with.');
      return;
    }

    if (this.selectedPickIds.length === 0) {
      this.toast.emit('Select at least one pick to trade.');
      return;
    }

    if (this.isSubmittingTrade) {
      return;
    }

    this.isSubmittingTrade = true;
    this.cdr.detectChanges();

    this.draftService
      .proposePickTrade(
        this.seasonId,
        String(this.selectedTradeClubId),
        this.selectedPickIds,
        this.tradeMessage.trim()
      )
      .pipe(
        finalize(() => {
          this.isSubmittingTrade = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: trade => {
          this.toast.emit(`⚡ Trade offer sent to ${trade.toClubName}.`);
          this.closeTradeModal();
          this.loadTradablePicks(true);
          this.refreshRequested.emit();
        },
        error: err => {
          console.error('Error sending trade offer:', err);

          this.toast.emit(
            err?.error?.message ??
            'Could not send trade offer.'
          );
        }
      });
  }

  acceptIncomingTrade(): void {
    if (!this.canRespondTrade) {
      return;
    }

    const tradeId = this.incomingTrade!.draftPickTradeId;

    this.isRespondingTrade = true;
    this.cdr.detectChanges();

    this.draftService
      .acceptPickTrade(this.seasonId, tradeId)
      .pipe(
        finalize(() => {
          this.isRespondingTrade = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: trade => {
          this.toast.emit(`✅ Trade accepted with ${trade.fromClubName}.`);
          this.incomingTradeCleared.emit();
          this.refreshRequested.emit();
          this.loadTradablePicks(true);
        },
        error: err => {
          console.error('Error accepting trade:', err);

          this.toast.emit(
            err?.error?.message ??
            'Could not accept trade.'
          );
        }
      });
  }

  rejectIncomingTrade(): void {
    if (!this.canRespondTrade) {
      return;
    }

    const tradeId = this.incomingTrade!.draftPickTradeId;

    this.isRespondingTrade = true;
    this.cdr.detectChanges();

    this.draftService
      .rejectPickTrade(this.seasonId, tradeId)
      .pipe(
        finalize(() => {
          this.isRespondingTrade = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: () => {
          this.toast.emit('❌ Trade rejected.');
          this.incomingTradeCleared.emit();
          this.refreshRequested.emit();
          this.loadTradablePicks(true);
        },
        error: err => {
          console.error('Error rejecting trade:', err);

          this.toast.emit(
            err?.error?.message ??
            'Could not reject trade.'
          );
        }
      });
  }
}
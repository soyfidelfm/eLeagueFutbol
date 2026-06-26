import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel
} from '@microsoft/signalr';
import { finalize, firstValueFrom, take } from 'rxjs';

import { DraftService } from '../../core/services/draft.service';
import { SeasonService } from '../../core/services/season.service';
import { API_ENDPOINTS } from '../../core/constants/api-endpoints';

import {
  DraftCenter,
  DraftOrder,
  DraftPick,
  DraftPickTrade
} from '../../core/models/draft.model';

import { DraftOrderBoard } from './components/draft-order-board/draft-order-board';
import { DraftPlayerList } from './components/draft-player-list/draft-player-list';
import { DraftSquadBoard } from './components/draft-squad-board/draft-squad-board';
import { DraftPickTrades } from './components/draft-pick-trades/draft-pick-trades';

@Component({
  selector: 'app-draft',
  standalone: true,
  imports: [
    CommonModule,
    DraftOrderBoard,
    DraftPlayerList,
    DraftSquadBoard,
    DraftPickTrades
  ],
  templateUrl: './draft.html',
  styleUrl: './draft.scss'
})
export class Draft implements OnInit, OnDestroy {
  isLoading = true;
  isStartingDraft = false;
  isLotteryRunning = false;

  errorMessage = '';

  secondsRemaining = 0;
  timerText = '00:00';

  lotteryDrawNumber = 0;

  draftCenter: DraftCenter | null = null;
  draftOrder: DraftOrder[] = [];
  lastPicks: DraftPick[] = [];

  incomingTrade: DraftPickTrade | null = null;

  seasonId = '';

  playerRefreshKey = 0;
  tradeRefreshKey = 0;

  private timerId?: number;
  private lotteryTimeoutId?: number;
  private hubConnection?: HubConnection;
  private isConnectingHub = false;

  constructor(
    private route: ActivatedRoute,
    private draftService: DraftService,
    private seasonService: SeasonService,
    private auth: AuthService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.route.queryParamMap
      .pipe(take(1))
      .subscribe(params => {
        this.seasonId = params.get('seasonId') ?? '';

        if (this.seasonId) {
          this.connectDraftHub();
          this.loadDraft();
          return;
        }

        this.loadCurrentSeasonAndDraft();
      });
  }

  ngOnDestroy(): void {
    if (this.timerId) {
      window.clearInterval(this.timerId);
    }

    if (this.lotteryTimeoutId) {
      window.clearTimeout(this.lotteryTimeoutId);
    }

    this.disconnectDraftHub();
  }

  get currentRound(): number {
    return this.draftCenter?.state.currentRound ?? 0;
  }

  get mySquadPlayers() {
    return this.draftCenter?.mySquad?.players ?? [];
  }
  get playersPerTeam(): number {
    return this.draftCenter?.state.playersPerTeam ?? 25;
  }

  get draftedCount(): number {
    return this.draftCenter?.mySquad.totalPlayers ?? 0;
  }

  get totalTeams(): number {
    return this.draftCenter?.state.totalTeams ?? 0;
  }

  get currentPickNumber(): number {
    return this.draftCenter?.state.currentPickNumber ?? 0;
  }

  get currentClubName(): string {
    return this.draftCenter?.state.currentClubName || 'Waiting...';
  }

  get isDraftActive(): boolean {
    return this.draftCenter?.state.isActive ?? false;
  }

  get currentBudget(): number {
    return this.draftCenter?.currentBudget ?? 0;
  }

  get isMyTurn(): boolean {
    if (!this.draftCenter) {
      return false;
    }

    return this.draftCenter.myUserId === this.draftCenter.state.currentUserId;
  }

  get lotteryTitle(): string {
    if (!this.isLotteryRunning) {
      return '';
    }

    return `Draft Lottery Draw ${this.lotteryDrawNumber} of 3`;
  }

  loadCurrentSeasonAndDraft(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.seasonService.getAll()
      .pipe(
        take(1),
        finalize(() => this.cdr.detectChanges())
      )
      .subscribe({
        next: seasons => {
          const currentSeason = (seasons ?? []).find(
            x => x.isCurrent && x.isActive
          );

          if (!currentSeason?.seasonId) {
            this.isLoading = false;
            this.errorMessage = 'No active season found.';
            this.cdr.detectChanges();
            return;
          }

          this.seasonId = currentSeason.seasonId;
          this.connectDraftHub();
          this.loadDraft();
        },
        error: err => {
          console.error('Error loading current season:', err);
          this.isLoading = false;
          this.errorMessage = 'Could not load current season.';
          this.cdr.detectChanges();
        }
      });
  }

  loadDraft(): void {
    if (!this.seasonId) {
      this.loadCurrentSeasonAndDraft();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.draftService.getCenter(this.seasonId)
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: center => {
          this.applyDraftCenter(center);
        },
        error: err => {
          console.error('Error loading draft:', err);
          this.errorMessage = err?.error?.message || 'Error loading draft.';
          this.cdr.detectChanges();
        }
      });
  }

  startDraft(): void {
    if (!this.seasonId || this.isLotteryRunning || this.isStartingDraft) {
      return;
    }

    this.errorMessage = '';

    if (this.draftOrder.length > 0) {
      this.isStartingDraft = true;

      this.draftService.startDraft(this.seasonId)
        .pipe(
          finalize(() => {
            this.isStartingDraft = false;
            this.cdr.detectChanges();
          })
        )
        .subscribe({
          next: center => {
            this.applyDraftCenter(center);
            this.showToast('🚀 Draft started.');
          },
          error: err => {
            console.error('Error starting draft:', err);
            this.errorMessage = err?.error?.message || 'Could not start draft.';
            this.cdr.detectChanges();
          }
        });

      return;
    }

    this.isLotteryRunning = true;
    this.isStartingDraft = true;
    this.lotteryDrawNumber = 1;

    this.showToast('🎲 Starting draft lottery...');
    this.runLotteryDraw(1);
  }

  private runLotteryDraw(drawNumber: number): void {
    this.lotteryDrawNumber = drawNumber;
    this.cdr.detectChanges();

    this.draftService.previewDraftOrder(this.seasonId, drawNumber)
      .subscribe({
        next: preview => {
          this.draftOrder = preview.draftOrder ?? [];
          this.lastPicks = [];
          this.cdr.detectChanges();

          if (drawNumber < 3) {
            this.showToast(`🎲 Draft lottery draw ${drawNumber} of 3...`);

            this.lotteryTimeoutId = window.setTimeout(() => {
              this.runLotteryDraw(drawNumber + 1);
            }, 5000);

            return;
          }

          const finalUserIds = this.draftOrder.map(x => x.userId);

          this.draftService.generateDraftOrder(this.seasonId, finalUserIds)
            .pipe(
              finalize(() => {
                this.isLotteryRunning = false;
                this.isStartingDraft = false;
                this.cdr.detectChanges();
              })
            )
            .subscribe({
              next: center => {
                this.applyDraftCenter(center);
                this.showToast('🏆 Final draft order saved. Ready to start.');
              },
              error: err => {
                console.error('Error saving final draft order:', err);
                this.errorMessage = err?.error?.message || 'Could not save final draft order.';
                this.cdr.detectChanges();
              }
            });
        },
        error: err => {
          console.error('Error generating draft order preview:', err);
          this.errorMessage = err?.error?.message || 'Could not generate draft order preview.';
          this.isLotteryRunning = false;
          this.isStartingDraft = false;
          this.cdr.detectChanges();
        }
      });
  }

  onChildRefreshRequested(): void {
    this.loadDraft();
  }

  onChildToast(message: string): void {
    this.showToast(message);
  }

  onIncomingTradeCleared(): void {
    this.incomingTrade = null;
  }

  private applyDraftCenter(center: DraftCenter): void {
    this.draftCenter = center;
    this.draftOrder = center.draftOrder ?? [];
    this.lastPicks = center.recentPicks ?? [];

    this.playerRefreshKey++;
    this.tradeRefreshKey++;

    this.startTimer(center.state.secondsRemaining);
    this.cdr.detectChanges();
  }

  private async connectDraftHub(): Promise<void> {
    if (!this.seasonId || this.isConnectingHub) {
      return;
    }

    if (this.hubConnection?.state === HubConnectionState.Connected) {
      await this.joinDraftGroup();
      return;
    }

    this.isConnectingHub = true;

    try {
      const apiBaseUrl = API_ENDPOINTS.draft.replace('/api/draft', '');
      const hubUrl = `${apiBaseUrl}/hubs/draft`;

      this.hubConnection = new HubConnectionBuilder()
        .withUrl(hubUrl, {
          accessTokenFactory: async () => {
            return await firstValueFrom(this.auth.getAccessTokenSilently());
          }
        })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Information)
        .build();

      this.registerDraftHubListeners();

      this.hubConnection.onreconnected(async () => {
        await this.joinDraftGroup();
        this.loadDraft();
      });

      await this.hubConnection.start();
      await this.joinDraftGroup();

      console.log('Draft SignalR connected.');
    } catch (err) {
      console.error('Error connecting Draft SignalR:', err);
    } finally {
      this.isConnectingHub = false;
      this.cdr.detectChanges();
    }
  }

  private async joinDraftGroup(): Promise<void> {
    if (!this.hubConnection || !this.seasonId) {
      return;
    }

    if (this.hubConnection.state !== HubConnectionState.Connected) {
      return;
    }

    try {
      await this.hubConnection.invoke('JoinDraft', this.seasonId);
    } catch (err) {
      console.error('Error joining draft group:', err);
    }
  }

  private registerDraftHubListeners(): void {
    if (!this.hubConnection) {
      return;
    }

    this.hubConnection.off('DraftTurnStarted');
    this.hubConnection.off('DraftPickMade');
    this.hubConnection.off('DraftCompleted');
    this.hubConnection.off('DraftPickTradeProposed');
    this.hubConnection.off('DraftPickTradeAccepted');
    this.hubConnection.off('DraftPickTradeRejected');
    this.hubConnection.off('DraftPickTradeCancelled');

    this.hubConnection.on('DraftTurnStarted', payload => {
      this.showToast(`⏱️ ${payload?.clubName ?? 'Next team'} is on the clock.`);
      this.loadDraft();
    });

    this.hubConnection.on('DraftPickMade', payload => {
      this.showToast(
        `✅ ${payload?.clubName ?? 'A club'} selected ${payload?.playerName ?? 'a player'}.`
      );

      this.loadDraft();
    });

    this.hubConnection.on('DraftCompleted', () => {
      this.showToast('🏁 Draft completed.');
      this.loadDraft();
    });

    this.hubConnection.on('DraftPickTradeProposed', (trade: DraftPickTrade) => {
      if (this.isIncomingTradeForMe(trade)) {
        this.incomingTrade = trade;
        this.showToast(`⚡ Trade offer from ${trade.fromClubName}`);
      } else {
        this.showToast(`⚡ New draft pick trade proposed: ${trade.fromClubName} → ${trade.toClubName}`);
      }

      this.tradeRefreshKey++;
      this.loadDraft();
      this.cdr.detectChanges();
    });

    this.hubConnection.on('DraftPickTradeAccepted', (trade: DraftPickTrade) => {
      this.showToast(`✅ Draft pick trade accepted: ${trade.fromClubName} → ${trade.toClubName}`);
      this.tradeRefreshKey++;
      this.loadDraft();
    });

    this.hubConnection.on('DraftPickTradeRejected', (trade: DraftPickTrade) => {
      this.showToast(`❌ Draft pick trade rejected: ${trade.fromClubName} → ${trade.toClubName}`);
      this.tradeRefreshKey++;
      this.loadDraft();
    });

    this.hubConnection.on('DraftPickTradeCancelled', (trade: DraftPickTrade) => {
      this.showToast(`🚫 Draft pick trade cancelled: ${trade.fromClubName} → ${trade.toClubName}`);
      this.tradeRefreshKey++;
      this.loadDraft();
    });
  }

  private async disconnectDraftHub(): Promise<void> {
    if (!this.hubConnection) {
      return;
    }

    try {
      if (this.hubConnection.state === HubConnectionState.Connected && this.seasonId) {
        await this.hubConnection.invoke('LeaveDraft', this.seasonId);
      }

      await this.hubConnection.stop();
    } catch (err) {
      console.error('Error disconnecting Draft SignalR:', err);
    }
  }

  private getMyClubName(): string {
    const myOrder = (this.draftCenter?.draftOrder ?? [])
      .find(team => team.userId === this.draftCenter?.myUserId);

    return myOrder?.clubName ?? '';
  }

  private getMyUserClubId(): string {
    const myOrder = (this.draftCenter?.draftOrder ?? [])
      .find(team => team.userId === this.draftCenter?.myUserId) as
      | (DraftOrder & { userClubId?: string })
      | undefined;

    return myOrder?.userClubId ?? '';
  }

  private isIncomingTradeForMe(trade: DraftPickTrade): boolean {
    const myUserClubId = this.getMyUserClubId();

    if (myUserClubId) {
      return trade.toUserClubId === myUserClubId;
    }

    const myClubName = this.getMyClubName();

    return !!myClubName && trade.toClubName === myClubName;
  }

  private showToast(message: string): void {
    this.errorMessage = message;
    this.cdr.detectChanges();

    setTimeout(() => {
      if (this.errorMessage === message) {
        this.errorMessage = '';
        this.cdr.detectChanges();
      }
    }, 3500);
  }

  private startTimer(seconds: number): void {
    if (this.timerId) {
      window.clearInterval(this.timerId);
    }

    this.secondsRemaining = Math.max(0, seconds);
    this.timerText = this.formatTime(this.secondsRemaining);
    this.cdr.detectChanges();

    if (!this.isDraftActive || this.secondsRemaining <= 0) {
      return;
    }

    this.timerId = window.setInterval(() => {
      this.secondsRemaining--;
      this.timerText = this.formatTime(this.secondsRemaining);
      this.cdr.detectChanges();

      if (this.secondsRemaining <= 0) {
        window.clearInterval(this.timerId);

        setTimeout(() => {
          this.loadDraft();
        }, 1200);
      }
    }, 1000);
  }

  private formatTime(totalSeconds: number): string {
    const safeSeconds = Math.max(0, totalSeconds);
    const minutes = Math.floor(safeSeconds / 60);
    const seconds = safeSeconds % 60;

    return `${minutes.toString().padStart(2, '0')}:${seconds
      .toString()
      .padStart(2, '0')}`;
  }
}
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from '../services/api.service.ts';
import { API_ENDPOINTS } from '../constants/api-endpoints';
import { TransferBoard, TransferOffer } from '../models/transfer.model';

@Injectable({
  providedIn: 'root'
})
export class TransferService extends ApiService {
  constructor(protected override http: HttpClient) {
    super(http);
  }

  getBoard(seasonId: string): Observable<TransferBoard> {
    return this.get<TransferBoard>(`${API_ENDPOINTS.transfers}/board?seasonId=${seasonId}`);
  }

  createOffer(seasonId: string, gamePlayerId: string, transferFee: number): Observable<TransferOffer> {
    return this.post<TransferOffer>(`${API_ENDPOINTS.transfers}/offers?seasonId=${seasonId}`, {
      gamePlayerId,
      transferFee
    });
  }

  acceptOffer(transferRecordId: string): Observable<TransferOffer> {
    return this.post<TransferOffer>(`${API_ENDPOINTS.transfers}/offers/${transferRecordId}/accept`, {});
  }

  rejectOffer(transferRecordId: string): Observable<TransferOffer> {
    return this.post<TransferOffer>(`${API_ENDPOINTS.transfers}/offers/${transferRecordId}/reject`, {});
  }

  cancelOffer(transferRecordId: string): Observable<void> {
    return this.post<void>(`${API_ENDPOINTS.transfers}/offers/${transferRecordId}/cancel`, {});
  }
}

// src/app/core/services/message.service.ts

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from './api.service.ts';
import { API_ENDPOINTS } from '../constants/api-endpoints';

import {
  ConversationDto,
  MessageDto,
  MessagePageDto,
  SendMessageDto
} from '../models/message.model';

@Injectable({
  providedIn: 'root'
})
export class MessageService extends ApiService {

  constructor(
    protected override http: HttpClient
  ) {
    super(http);
  }

  getConversations(): Observable<ConversationDto[]> {
    return this.get<ConversationDto[]>(
      `${API_ENDPOINTS.messages}/conversations`
    );
  }

  /**
   * Loads one page of a conversation (oldest to newest).
   * Omit `beforeMessageId` for the latest page; pass the oldest loaded id to get older messages.
   */
  getConversation(
    userId: string,
    beforeMessageId?: string | null,
    take = 15
  ): Observable<MessagePageDto> {
    let params = new HttpParams().set('take', take);

    if (beforeMessageId) {
      params = params.set('before', beforeMessageId);
    }

    return this.get<MessagePageDto>(
      `${API_ENDPOINTS.messages}/conversation/${userId}`,
      params
    );
  }

  sendMessage(
    dto: SendMessageDto
  ): Observable<MessageDto> {
    return this.post<MessageDto>(
      API_ENDPOINTS.messages,
      dto
    );
  }

  markAsRead(
    messageId: string
  ): Observable<void> {
    return this.put<void>(
      `${API_ENDPOINTS.messages}/${messageId}/read`,
      {}
    );
  }
}
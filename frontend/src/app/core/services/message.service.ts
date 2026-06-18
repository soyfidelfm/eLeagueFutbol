// src/app/core/services/message.service.ts

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from './api.service.ts.js';
import { API_ENDPOINTS } from '../constants/api-endpoints.js';

import {
  ConversationDto,
  MessageDto,
  SendMessageDto
} from '../models/message.model.js';

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

  getConversation(
    userId: string
  ): Observable<MessageDto[]> {
    return this.get<MessageDto[]>(
      `${API_ENDPOINTS.messages}/conversation/${userId}`
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
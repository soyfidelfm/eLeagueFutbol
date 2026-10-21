export interface SendMessageDto {
  receiverUserId: string;
  text: string;
}

export interface MessageDto {
  messageId: string;
  senderUserId: string;
  senderName: string;
  receiverUserId: string;
  receiverName: string;
  text: string;
  isMine: boolean;
  isRead: boolean;
  sentAtUTC: string;
}

export interface ConversationDto {
  userId: string;
  displayName: string;
  avatarUrl?: string | null;
  lastMessage: string;
  lastMessageAtUTC: string;
  unreadCount: number;
}

export interface MessagePageDto {
  items: MessageDto[];
  hasMore: boolean;
}

export interface TypingEvent {
  senderUserId: string;
}

export interface PresenceUser {
  userId: string;
  displayName: string;
  avatarUrl?: string | null;
  lastSeenAtUTC?: string | null;
}

export interface DraftRealtimeEvent {
  seasonId: string;
  eventType: string;
}

export interface MessageReadEvent {
  messageId: string;
  readerUserId: string;
}

export interface RealtimeMessageReceived {
  messageId: string;
  senderUserId: string;
  senderName: string;
  senderAvatarUrl?: string;
  receiverUserId: string;
  text: string;
  isRead: boolean;
  sentAtUTC: string;
}

export interface RealtimeNotificationReceived {
  notificationId: string;
  title: string;
  message: string;
  type: string;
  url?: string;
  isRead: boolean;
  createdAtUTC: string;
}

export interface RealtimeNotificationRead {
  notificationId: string;
  unreadCount: number;
}

export interface RealtimeDraftTurnStarted {
  seasonId: string;
  pickNumber: number;
  clubName: string;
  logoUrl?: string;
  userId: string;
}

export interface RealtimeDraftPickMade {
  seasonId: string;
  pickNumber: number;
  roundNumber: number;
  clubName: string;
  logoUrl?: string;
  playerName: string;
  playerImageUrl?: string;
  overall: number;
  position: string;
}

export interface RealtimeDraftCompleted {
  seasonId: string;
}
export interface Notification {
  notificationId: string;

  title: string;
  message: string;

  type: string;

  url?: string;

  isRead: boolean;

  createdAtUTC: string;
}

export interface UnreadNotificationCount {
  count: number;
}
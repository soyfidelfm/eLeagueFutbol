import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { initializeApp } from 'firebase/app';
import {
  getMessaging,
  getToken,
  isSupported,
  onMessage
} from 'firebase/messaging';

import {
  FIREBASE_CONFIG,
  FIREBASE_VAPID_KEY
} from '../constants/firebase.constants';

import { API_ENDPOINTS } from '../constants/api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private app = initializeApp(FIREBASE_CONFIG);
  private isListening = false;

  constructor(private http: HttpClient) {}

  async requestPermissionAndGetToken(): Promise<string | null> {
    const supported = await isSupported();

    if (!supported) {
      console.warn('Firebase messaging is not supported on this browser.');
      return null;
    }

    const permission = await Notification.requestPermission();

    if (permission !== 'granted') {
      console.warn('Notification permission was not granted.');
      return null;
    }

    const messaging = getMessaging(this.app);

    const token = await getToken(messaging, {
      vapidKey: FIREBASE_VAPID_KEY
    });

    console.log('FCM Token:', token);

    return token;
  }

  saveToken(token: string) {
  return this.http.post(`${API_ENDPOINTS.notifications}/token`, {
    token: token,
    deviceType: 'web'
  });
}

  listenForMessages(): void {
    if (this.isListening) {
      return;
    }

    this.isListening = true;

    isSupported().then(supported => {
      if (!supported) {
        return;
      }

      const messaging = getMessaging(this.app);

      onMessage(messaging, payload => {
        console.log('Foreground message:', payload);

        window.dispatchEvent(
          new CustomEvent('eleague-message-received', {
            detail: payload
          })
        );

        window.dispatchEvent(
          new CustomEvent('eleague-notification-received', {
            detail: payload
          })
        );
      });
    });
  }
}
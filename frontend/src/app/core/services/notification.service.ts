import { Injectable } from '@angular/core';
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

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private app = initializeApp(FIREBASE_CONFIG);
  private isListening = false;

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

        const type = payload.data?.['type'];

        if (type === 'message') {
          window.dispatchEvent(
            new CustomEvent('eleague-message-received', {
              detail: payload
            })
          );
        }

        window.dispatchEvent(
          new CustomEvent('eleague-notification-received', {
            detail: payload
          })
        );
      });
    });
  }
}
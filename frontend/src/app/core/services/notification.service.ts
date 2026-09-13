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

const SW_URL = '/firebase-messaging-sw.js';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private app = initializeApp(FIREBASE_CONFIG);
  private isListening = false;
  private registration?: ServiceWorkerRegistration;

  constructor(private http: HttpClient) { }

  async requestPermissionAndGetToken(): Promise<string | null> {
    const supported = await isSupported();

    if (!supported) {
      console.warn('Firebase messaging is not supported on this browser.');
      return null;
    }

    if (!('Notification' in window) || !('serviceWorker' in navigator)) {
      console.warn('Notifications are not supported on this browser.');
      return null;
    }

    let permission = Notification.permission;

    if (permission === 'default') {
      permission = await Notification.requestPermission();
    }

    if (permission !== 'granted') {
      console.warn('Notification permission was not granted:', permission);
      return null;
    }

    const registration = await this.ensureServiceWorker();

    if (!registration) {
      return null;
    }

    const messaging = getMessaging(this.app);

    const token = await getToken(messaging, {
      vapidKey: FIREBASE_VAPID_KEY,
      serviceWorkerRegistration: registration
    });

    if (!token) {
      console.warn('FCM token was not generated.');
      return null;
    }

    return token;
  }

  saveToken(token: string) {
    return this.http.post(`${API_ENDPOINTS.notifications}/token`, {
      token,
      deviceType: 'web'
    });
  }

  listenForMessages(): void {
    if (this.isListening) {
      return;
    }

    this.isListening = true;

    void this.ensureServiceWorker();

    isSupported().then(supported => {
      if (!supported) {
        return;
      }

      const messaging = getMessaging(this.app);

      onMessage(messaging, payload => {
        const title =
          payload.notification?.title ||
          payload.data?.['title'] ||
          'eLeagueFutbol';

        const body =
          payload.notification?.body ||
          payload.data?.['body'] ||
          '';

        const url =
          payload.data?.['url'] ||
          payload.fcmOptions?.link ||
          '/dashboard';

        if (Notification.permission === 'granted' && (title || body)) {
          const popup = new Notification(title, {
            body,
            icon: '/images/iOSicon.png',
            badge: '/images/iOSicon.png',
            data: { url }
          });

          popup.onclick = () => {
            window.focus();
            if (url) {
              window.location.assign(url);
            }
            popup.close();
          };
        }

        window.dispatchEvent(
          new CustomEvent('eleague-notification-received', {
            detail: payload
          })
        );
      });
    });
  }

  private async ensureServiceWorker(): Promise<ServiceWorkerRegistration | undefined> {
    if (this.registration) {
      return this.registration;
    }

    if (!('serviceWorker' in navigator)) {
      return undefined;
    }

    this.registration = await navigator.serviceWorker.register(SW_URL, {
      scope: '/'
    });

    await navigator.serviceWorker.ready;
    return this.registration;
  }
}

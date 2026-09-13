importScripts('https://www.gstatic.com/firebasejs/12.15.0/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/12.15.0/firebase-messaging-compat.js');

firebase.initializeApp({
  apiKey: 'AIzaSyA47dU6qLj7ohgZ1LyG6aGbnQRerOpD6D8',
  authDomain: 'eleaguefutbol.firebaseapp.com',
  projectId: 'eleaguefutbol',
  storageBucket: 'eleaguefutbol.firebasestorage.app',
  messagingSenderId: '617676016686',
  appId: '1:617676016686:web:aba9548ba2c2b15cd2a3bf',
  measurementId: 'G-HD621ZX5FG'
});

const messaging = firebase.messaging();

messaging.onBackgroundMessage((payload) => {
  const title =
    payload.notification?.title ||
    payload.data?.title ||
    'eLeagueFutbol';

  const body =
    payload.notification?.body ||
    payload.data?.body ||
    '';

  // Data-only payloads need a manual notification.
  // Notification payloads are already displayed by the browser.
  if (payload.notification) {
    return;
  }

  return self.registration.showNotification(title, {
    body,
    icon: '/images/iOSicon.png',
    badge: '/images/iOSicon.png',
    data: payload.data || {}
  });
});

self.addEventListener('notificationclick', (event) => {
  event.notification.close();

  const target =
    event.notification.data?.url ||
    event.notification.data?.link ||
    '/dashboard';

  event.waitUntil(
    self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then((clients) => {
      for (const client of clients) {
        if ('focus' in client) {
          client.focus();
          if ('navigate' in client) {
            client.navigate(target);
          }
          return;
        }
      }

      if (self.clients.openWindow) {
        return self.clients.openWindow(target);
      }
    })
  );
});

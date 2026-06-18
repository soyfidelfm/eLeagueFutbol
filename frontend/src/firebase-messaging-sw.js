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
  console.log('Background message received:', payload);

  const notificationTitle =
    payload.notification?.title ?? 'eLeagueFutbol';

  const notificationOptions = {
    body: payload.notification?.body ?? '',
    icon: '/images/iOSicon.png',
    badge: '/images/iOSicon.png',
    data: payload.data
  };

  self.registration.showNotification(
    notificationTitle,
    notificationOptions
  );
});
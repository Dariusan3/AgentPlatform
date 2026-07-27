/**
 * Service worker pentru notificari de sistem.
 *
 * Ruleaza separat de pagina si supravietuieste inchiderii tabului — de asta
 * notificarea ajunge chiar daca aplicatia nu e deschisa. Nu are acces la starea
 * React, deci tot ce ii trebuie vine in corpul mesajului.
 */

self.addEventListener('install', () => {
  // Fara asta, versiunea noua asteapta inchiderea tuturor taburilor vechi
  self.skipWaiting()
})

self.addEventListener('activate', (event) => {
  event.waitUntil(self.clients.claim())
})

self.addEventListener('push', (event) => {
  let payload = {}
  try {
    payload = event.data ? event.data.json() : {}
  } catch {
    // Un mesaj ilizibil nu trebuie sa lase utilizatorul fara nimic
    payload = { title: 'Notificare', body: 'A apărut ceva nou în Portar.' }
  }

  const title = payload.title || 'Portar'

  event.waitUntil(
    self.registration.showNotification(title, {
      body: payload.body || '',
      icon: '/favicon.svg',
      badge: '/favicon.svg',
      // Notificarile de acelasi fel se inlocuiesc, nu se aduna in stiva
      tag: payload.severity === 'error' ? 'portar-eroare' : 'portar',
      renotify: true,
      data: { url: payload.url || '/dashboard' },
    }),
  )
})

self.addEventListener('notificationclick', (event) => {
  event.notification.close()
  const target = (event.notification.data && event.notification.data.url) || '/dashboard'

  event.waitUntil(
    self.clients
      .matchAll({ type: 'window', includeUncontrolled: true })
      .then((windows) => {
        // Un tab deschis se refoloseste: altfel fiecare click lasa in urma
        // inca o fereastra cu aceeasi aplicatie
        for (const client of windows) {
          if (client.url.includes(self.location.origin) && 'focus' in client) {
            client.navigate(target)
            return client.focus()
          }
        }

        return self.clients.openWindow(target)
      }),
  )
})

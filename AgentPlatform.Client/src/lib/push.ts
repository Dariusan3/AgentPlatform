import api, { apiErrorMessage } from '@/lib/api'

/**
 * Abonarea browserului la notificari de sistem.
 *
 * Trei lucruri trebuie sa se alinieze: service workerul inregistrat, permisiunea
 * data de utilizator si abonamentul trimis serverului. Daca oricare lipseste,
 * notificarile tac fara nicio eroare vizibila — de aceea fiecare pas de mai jos
 * intoarce un motiv in romana, nu doar `false`.
 */

export type PushStatus =
  | 'unsupported'
  | 'denied'
  | 'unsubscribed'
  | 'subscribed'

/** Cheia VAPID vine base64url; API-ul browserului cere octeti. */
function base64UrlToBytes(base64Url: string): ArrayBuffer {
  const padded = (base64Url + '='.repeat((4 - (base64Url.length % 4)) % 4))
    .replace(/-/g, '+')
    .replace(/_/g, '/')

  const binary = atob(padded)
  const buffer = new ArrayBuffer(binary.length)
  const bytes = new Uint8Array(buffer)
  for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i)
  return buffer
}

/** Cheile abonamentului sunt ArrayBuffer; serverul le vrea base64url. */
function bufferToBase64Url(buffer: ArrayBuffer | null): string {
  if (!buffer) return ''

  const bytes = new Uint8Array(buffer)
  let binary = ''
  for (let i = 0; i < bytes.length; i++) binary += String.fromCharCode(bytes[i])

  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
}

export function pushSupported(): boolean {
  return (
    'serviceWorker' in navigator &&
    'PushManager' in window &&
    'Notification' in window
  )
}

async function registration(): Promise<ServiceWorkerRegistration> {
  const existing = await navigator.serviceWorker.getRegistration('/sw.js')
  if (existing) return existing

  return navigator.serviceWorker.register('/sw.js')
}

export async function currentStatus(): Promise<PushStatus> {
  if (!pushSupported()) return 'unsupported'
  if (Notification.permission === 'denied') return 'denied'

  const worker = await navigator.serviceWorker.getRegistration('/sw.js')
  const subscription = await worker?.pushManager.getSubscription()

  return subscription ? 'subscribed' : 'unsubscribed'
}

/**
 * Cere permisiunea, se aboneaza si trimite abonamentul serverului.
 * Aruncă cu un mesaj gata de afișat daca ceva nu merge.
 */
export async function subscribe(publicKey: string): Promise<void> {
  if (!pushSupported()) {
    throw new Error('Browserul nu acceptă notificări de sistem.')
  }

  const permission = await Notification.requestPermission()
  if (permission !== 'granted') {
    throw new Error(
      'Notificările au fost refuzate. Le poți reactiva din setările site-ului, ' +
        'de lângă bara de adresă.',
    )
  }

  const worker = await registration()

  // `ready` asteapta activarea: `subscribe` pe un worker inca in instalare esueaza
  await navigator.serviceWorker.ready

  const subscription = await worker.pushManager.subscribe({
    // Obligatoriu true in toate browserele actuale: nu se accepta push tacut
    userVisibleOnly: true,
    applicationServerKey: base64UrlToBytes(publicKey),
  })

  try {
    await api.post('/api/notifications/push/subscribe', {
      endpoint: subscription.endpoint,
      p256dh: bufferToBase64Url(subscription.getKey('p256dh')),
      auth: bufferToBase64Url(subscription.getKey('auth')),
    })
  } catch (error) {
    // Serverul explica in romana (ex. adresa revendicata de alt cont); fara asta
    // utilizatorul ar vedea „Request failed with status code 409”.
    throw new Error(apiErrorMessage(error))
  }
}

export async function unsubscribe(): Promise<void> {
  const worker = await navigator.serviceWorker.getRegistration('/sw.js')
  const subscription = await worker?.pushManager.getSubscription()
  if (!subscription) return

  // Serverul intai: daca browserul se dezaboneaza si cererea eșueaza, randul ar
  // ramane in DB si am trimite catre un abonament care nu mai exista.
  await api.post('/api/notifications/push/unsubscribe', {
    endpoint: subscription.endpoint,
    p256dh: bufferToBase64Url(subscription.getKey('p256dh')),
    auth: bufferToBase64Url(subscription.getKey('auth')),
  })

  await subscription.unsubscribe()
}

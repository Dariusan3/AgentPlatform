import { createClient } from '@supabase/supabase-js'

const url = import.meta.env.VITE_SUPABASE_URL
const anonKey = import.meta.env.VITE_SUPABASE_ANON_KEY

// Mai bine cadem la pornire, cu mesaj clar, decat sa dam 401 la primul login
if (!url || !anonKey) {
  throw new Error(
    'Lipsesc VITE_SUPABASE_URL sau VITE_SUPABASE_ANON_KEY. ' +
      'Adauga-le in AgentPlatform.Client/.env.local si reporneste `npm run dev`.',
  )
}

/**
 * Cheia publishable (anon) e publica prin design - protectia reala vine din RLS.
 * Secret key-ul nu are ce sa caute aici niciodata: ocoleste RLS.
 */
export const supabase = createClient(url, anonKey, {
  auth: {
    persistSession: true,
    autoRefreshToken: true,
    detectSessionInUrl: true,
  },
})

/**
 * signInWithOAuth nu verifica daca providerul e activ: construieste URL-ul si
 * redirecteaza, iar Supabase raspunde 400 cu o pagina bruta. Ca sa nu ajunga
 * nimeni acolo, intrebam o data ce providere sunt pornite.
 */
export async function fetchEnabledProviders(): Promise<Set<string>> {
  try {
    const response = await fetch(`${url}/auth/v1/settings`, {
      headers: { apikey: anonKey },
    })
    if (!response.ok) return new Set()

    const settings: { external?: Record<string, boolean> } =
      await response.json()

    return new Set(
      Object.entries(settings.external ?? {})
        .filter(([, enabled]) => enabled)
        .map(([provider]) => provider),
    )
  } catch {
    // Fara reteaua asta butoanele OAuth ramin dezactivate, ceea ce e corect
    return new Set()
  }
}

export default supabase

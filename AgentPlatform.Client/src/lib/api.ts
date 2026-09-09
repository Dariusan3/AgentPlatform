import axios from 'axios'
import type { AxiosError, InternalAxiosRequestConfig } from 'axios'
import { toast } from 'sonner'
import supabase from '@/lib/supabaseClient'

const baseURL = import.meta.env.VITE_API_URL ?? 'http://localhost:5274'

export const api = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
  // Raspunsul agentului trece prin Groq, deci poate dura. Fara nicio limita insa,
  // o cerere pierduta ar tine spinnerul pe ecran la nesfarsit.
  timeout: 90_000,
})

/**
 * Tokenul se ia din sesiunea Supabase la fiecare cerere, nu o data la pornire:
 * supabase-js il reinnoieste singur, iar unul memorat ar expira in o ora.
 */
api.interceptors.request.use(async (config) => {
  const { data } = await supabase.auth.getSession()
  const token = data.session?.access_token

  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

/** Cererile pe care le-am reincercat deja dupa un refresh, ca sa nu intram in bucla. */
type RetriedConfig = InternalAxiosRequestConfig & { _retriedAfterRefresh?: boolean }

/**
 * Un 401 inseamna aproape mereu ca tokenul a expirat intre doua taburi sau dupa
 * ce laptopul a stat inchis. Incercam o data sa reinnoim sesiunea si repetam
 * cererea; daca nici asa nu merge, sesiunea chiar s-a dus si scoatem utilizatorul
 * afara, in loc sa il lasam pe un ecran care da erori la fiecare click.
 */
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const config = error.config as RetriedConfig | undefined

    if (error.response?.status !== 401 || !config || config._retriedAfterRefresh) {
      return Promise.reject(error)
    }

    config._retriedAfterRefresh = true

    const { data, error: refreshError } = await supabase.auth.refreshSession()

    if (!refreshError && data.session) {
      config.headers.Authorization = `Bearer ${data.session.access_token}`
      return api.request(config)
    }

    await forceSignOut()
    return Promise.reject(error)
  },
)

/** Rulam o singura data, chiar daca pica mai multe cereri deodata. */
let signingOut = false

async function forceSignOut() {
  if (signingOut) return
  signingOut = true

  toast.error('Sesiunea a expirat. Conectează-te din nou.')
  await supabase.auth.signOut()

  // Reincarcare completa: golim si cache-ul de query-uri, si starea din pagini
  if (window.location.pathname !== '/login') {
    window.location.assign('/login')
  } else {
    signingOut = false
  }
}

/** Forma erorilor din ExceptionMiddleware */
type ApiErrorBody = {
  error?: string
  statusCode?: number
  timestamp?: string
  errors?: Record<string, string[]>
}

/**
 * Scoate mesajul in romana pe care backendul l-a trimis deja. Fara asta,
 * utilizatorul ar vedea "Request failed with status code 400".
 */
export function apiErrorMessage(error: unknown): string {
  const axiosError = error as AxiosError<ApiErrorBody>

  if (axiosError?.response) {
    const body = axiosError.response.data

    // Detaliile per camp sunt mai utile decat mesajul general
    const fieldError = body?.errors
      ? Object.values(body.errors).flat().find(Boolean)
      : undefined

    if (fieldError) return fieldError
    if (body?.error) return body.error

    // Backendul trimite mereu un mesaj; ajungem aici doar daca a raspuns altcineva
    // (un proxy, ngrok, un tunel cazut), deci explicam statusul pe scurt.
    switch (axiosError.response.status) {
      case 401:
        return 'Sesiunea a expirat. Conectează-te din nou.'
      case 403:
        return 'Nu ai acces la resursa asta.'
      case 404:
        return 'Ruta cerută nu există pe server. Repornește API-ul dacă tocmai ai adăugat-o.'
      case 405:
        return 'Serverul nu acceptă această metodă pe ruta cerută. ' +
          'De obicei înseamnă că rulează o versiune veche a API-ului — repornește-l.'
      case 502:
      case 503:
      case 504:
        return 'Serverul nu e disponibil acum. Încearcă din nou în câteva momente.'
      default:
        return `Serverul a răspuns ${axiosError.response.status}.`
    }
  }

  if (axiosError?.code === 'ECONNABORTED') {
    return 'Serverul nu a răspuns la timp. Încearcă din nou.'
  }

  if (typeof navigator !== 'undefined' && navigator.onLine === false) {
    return 'Pare că nu ai conexiune la internet.'
  }

  if (axiosError?.request) {
    return 'Nu am putut contacta serverul. Verifică dacă API-ul rulează pe ' +
      `${baseURL}.`
  }

  return 'Ceva nu a funcționat. Încearcă din nou.'
}

export default api

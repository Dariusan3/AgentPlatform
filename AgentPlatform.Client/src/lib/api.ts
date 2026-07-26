import axios from 'axios'
import type { AxiosError } from 'axios'
import supabase from '@/lib/supabaseClient'

const baseURL = import.meta.env.VITE_API_URL ?? 'http://localhost:5274'

export const api = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
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

    if (axiosError.response.status === 401) {
      return 'Sesiunea a expirat. Conectează-te din nou.'
    }
    return `Serverul a răspuns ${axiosError.response.status}.`
  }

  if (axiosError?.request) {
    return 'Nu am putut contacta serverul. Verifică dacă API-ul rulează.'
  }

  return 'Ceva nu a funcționat. Încearcă din nou.'
}

export default api

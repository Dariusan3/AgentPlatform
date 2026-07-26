import { createContext } from 'react'
import type { Session, User } from '@supabase/supabase-js'

export type AuthResult = { error: string | null }

export type SignUpResult = AuthResult & {
  /** true cand Supabase are confirmarea pe email activa si nu a returnat sesiune */
  needsConfirmation: boolean
}

export type AuthContextValue = {
  user: User | null
  session: Session | null
  /** true doar cat verificam sesiunea initiala din localStorage */
  loading: boolean
  signIn: (email: string, password: string) => Promise<AuthResult>
  signUp: (
    email: string,
    password: string,
    fullName: string,
  ) => Promise<SignUpResult>
  /** La succes browserul pleaca spre Google, deci nu se mai intoarce nimic util */
  signInWithGoogle: () => Promise<AuthResult>
  /** Citit din /auth/v1/settings, ca butonul sa nu trimita spre un provider inchis */
  googleEnabled: boolean
  signOut: () => Promise<void>
}

/**
 * Contextul sta separat de provider ca fast refresh sa nu se rupa:
 * un fisier .tsx care exporta si componente si non-componente il strica.
 */
export const AuthContext = createContext<AuthContextValue | null>(null)

import { useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import type { Session } from '@supabase/supabase-js'
import { useNavigate } from 'react-router-dom'
import { AuthContext } from '@/context/auth-context'
import type { AuthResult, SignUpResult } from '@/context/auth-context'
import { translateAuthError } from '@/lib/authErrors'
import supabase, { fetchEnabledProviders } from '@/lib/supabaseClient'

export function AuthProvider({ children }: { children: ReactNode }) {
  const navigate = useNavigate()
  const [session, setSession] = useState<Session | null>(null)
  const [loading, setLoading] = useState(true)
  const [providers, setProviders] = useState<Set<string>>(() => new Set())

  useEffect(() => {
    let active = true
    fetchEnabledProviders().then((enabled) => {
      if (active) setProviders(enabled)
    })
    return () => {
      active = false
    }
  }, [])

  useEffect(() => {
    let active = true

    // Sesiunea initiala vine din localStorage, deci e o operatie asincrona scurta
    supabase.auth.getSession().then(({ data }) => {
      if (!active) return
      setSession(data.session)
      setLoading(false)
    })

    // Reactualizeaza user-ul la login, logout, refresh de token si confirmare email
    const { data } = supabase.auth.onAuthStateChange((_event, nextSession) => {
      setSession(nextSession)
      setLoading(false)
    })

    return () => {
      active = false
      data.subscription.unsubscribe()
    }
  }, [])

  const signIn = useCallback(
    async (email: string, password: string): Promise<AuthResult> => {
      const { error } = await supabase.auth.signInWithPassword({
        email: email.trim(),
        password,
      })
      return { error: error ? translateAuthError(error) : null }
    },
    [],
  )

  const signUp = useCallback(
    async (
      email: string,
      password: string,
      fullName: string,
    ): Promise<SignUpResult> => {
      const { data, error } = await supabase.auth.signUp({
        email: email.trim(),
        password,
        options: {
          // Trigger-ul on_auth_user_created citeste full_name din raw_user_meta_data
          data: { full_name: fullName.trim() },
          emailRedirectTo: `${window.location.origin}/login`,
        },
      })

      if (error) {
        return { error: translateAuthError(error), needsConfirmation: false }
      }

      // Cand emailul e deja folosit, Supabase intoarce un user fara identities
      // in loc de eroare, ca sa nu permita enumerarea conturilor.
      if (data.user && data.user.identities?.length === 0) {
        return {
          error: 'Există deja un cont cu acest email.',
          needsConfirmation: false,
        }
      }

      return { error: null, needsConfirmation: !data.session }
    },
    [],
  )

  const signInWithGoogle = useCallback(async (): Promise<AuthResult> => {
    // Ne intoarcem pe /login, nu direct pe /dashboard: e ruta publica, deci un
    // schimb de cod eșuat lasa utilizatorul pe o pagina utila, iar la succes
    // PublicOnlyRoute il trimite singur mai departe.
    const { error } = await supabase.auth.signInWithOAuth({
      provider: 'google',
      options: { redirectTo: `${window.location.origin}/login` },
    })
    return { error: error ? translateAuthError(error) : null }
  }, [])

  const signOut = useCallback(async () => {
    // Navigam intai: daca golim sesiunea cat suntem inca sub ProtectedRoute,
    // acesta redirecteaza spre /login si ne fura destinatia.
    navigate('/', { replace: true })
    await supabase.auth.signOut()
  }, [navigate])

  const value = useMemo(
    () => ({
      user: session?.user ?? null,
      session,
      loading,
      signIn,
      signUp,
      signInWithGoogle,
      googleEnabled: providers.has('google'),
      signOut,
    }),
    [session, loading, providers, signIn, signUp, signInWithGoogle, signOut],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

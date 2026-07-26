import { useContext } from 'react'
import { AuthContext } from '@/context/auth-context'

/** Starea sta in AuthProvider, nu aici: altfel fiecare componenta ar avea alta sesiune. */
export function useAuth() {
  const context = useContext(AuthContext)

  if (!context) {
    throw new Error('useAuth trebuie folosit inauntrul <AuthProvider>.')
  }

  return context
}

import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { FullPageSpinner } from '@/components/Spinner'
import { useAuth } from '@/hooks/useAuth'

/** Lasa sa treaca doar utilizatorii autentificati. */
export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { user, loading } = useAuth()
  const location = useLocation()

  if (loading) return <FullPageSpinner />

  if (!user) {
    // Retinem unde voia sa ajunga, ca sa il ducem acolo dupa login
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  return children
}

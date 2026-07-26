import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { FullPageSpinner } from '@/components/Spinner'
import { useAuth } from '@/hooks/useAuth'

/** Invers fata de ProtectedRoute: /login si /signup nu au sens daca esti deja logat. */
export function PublicOnlyRoute({ children }: { children: ReactNode }) {
  const { user, loading } = useAuth()

  if (loading) return <FullPageSpinner />
  if (user) return <Navigate to="/dashboard" replace />

  return children
}

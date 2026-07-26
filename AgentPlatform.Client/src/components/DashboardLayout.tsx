import { useState } from 'react'
import { Link, Outlet } from 'react-router-dom'
import { Logo } from '@/components/Logo'
import { Spinner } from '@/components/Spinner'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/hooks/useAuth'

export function DashboardLayout() {
  const { user, signOut } = useAuth()
  const [leaving, setLeaving] = useState(false)

  const handleSignOut = async () => {
    setLeaving(true)
    await signOut()
  }

  return (
    <div className="min-h-svh">
      {/* Deconectarea sta in layout, nu in pagina: e nevoie de ea pe tot /dashboard/* */}
      <header className="border-line bg-ink/70 sticky top-0 z-50 border-b backdrop-blur-xl">
        <div className="mx-auto flex h-16 max-w-6xl items-center justify-between gap-4 px-5 sm:px-8">
          <Link to="/dashboard">
            <Logo />
          </Link>

          <div className="flex items-center gap-4">
            <span className="text-muted hidden font-mono text-[11px] sm:inline">
              {user?.email}
            </span>
            <Button
              variant="outline"
              size="sm"
              onClick={handleSignOut}
              disabled={leaving}
            >
              {leaving && <Spinner className="size-3.5" />}
              Deconectează-te
            </Button>
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-6xl px-5 py-12 sm:px-8">
        <Outlet />
      </main>
    </div>
  )
}

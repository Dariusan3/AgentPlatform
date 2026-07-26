import { useEffect, useState } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { Sidebar } from '@/components/layout/Sidebar'
import { TopBar } from '@/components/layout/TopBar'
import { TooltipProvider } from '@/components/ui/tooltip'

const STORAGE_KEY = 'portar:sidebar-collapsed'

export function DashboardLayout() {
  const { pathname } = useLocation()
  // Starea restrânsa se ţine minte: e o preferinta, nu ceva de reales la fiecare load
  const [collapsed, setCollapsed] = useState(
    () => localStorage.getItem(STORAGE_KEY) === '1',
  )
  /**
   * Overlay-ul e derivat din ruta pe care a fost deschis, nu sincronizat cu ea:
   * orice navigare il inchide singur, fara effect si fara randari in cascada.
   */
  const [openedOn, setOpenedOn] = useState<string | null>(null)
  const mobileOpen = openedOn === pathname
  const setMobileOpen = (open: boolean) =>
    setOpenedOn(open ? pathname : null)

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, collapsed ? '1' : '0')
  }, [collapsed])

  useEffect(() => {
    if (!mobileOpen) return
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setOpenedOn(null)
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [mobileOpen])

  return (
    <TooltipProvider delayDuration={200}>
      <div className="flex min-h-svh">
        {/* Desktop: coloana fixa. Sub lg dispare complet. */}
        <aside className="sticky top-0 hidden h-svh shrink-0 lg:block">
          <Sidebar
            collapsed={collapsed}
            onToggleCollapsed={() => setCollapsed((value) => !value)}
          />
        </aside>

        {/* Mobil: overlay peste tot, se inchide la click in afara */}
        {mobileOpen && (
          <div className="fixed inset-0 z-50 lg:hidden">
            <button
              type="button"
              aria-label="Închide meniul"
              onClick={() => setMobileOpen(false)}
              className="animate-overlay-in absolute inset-0 bg-black/70 backdrop-blur-sm"
            />
            <div className="animate-sheet-in-left absolute inset-y-0 left-0">
              <Sidebar
                variant="overlay"
                collapsed={false}
                onToggleCollapsed={() => setMobileOpen(false)}
                onNavigate={() => setMobileOpen(false)}
              />
            </div>
          </div>
        )}

        <div className="flex min-w-0 flex-1 flex-col">
          <TopBar onOpenMenu={() => setMobileOpen(true)} />
          <main className="min-w-0 flex-1 px-4 py-6 sm:px-6 sm:py-8">
            <Outlet />
          </main>
        </div>
      </div>
    </TooltipProvider>
  )
}

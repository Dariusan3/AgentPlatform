import { useState } from 'react'
import { ChevronLeft, ChevronRight, LogOut, X } from 'lucide-react'
import { Link, NavLink } from 'react-router-dom'
import { LogoMark } from '@/components/Logo'
import { navItems } from '@/components/layout/nav-items'
import { Spinner } from '@/components/Spinner'
import { Avatar } from '@/components/ui/avatar'
import { useAuth } from '@/hooks/useAuth'
import { BRAND } from '@/lib/brand'
import { cn } from '@/lib/utils'

type SidebarProps = {
  collapsed: boolean
  onToggleCollapsed: () => void
  /** Pe mobil sidebarul e overlay, deci navigarea trebuie sa il inchida */
  onNavigate?: () => void
  /** In overlay butonul inchide, nu restrange - si trebuie vizibil pe mobil */
  variant?: 'desktop' | 'overlay'
}

export function Sidebar({
  collapsed,
  onToggleCollapsed,
  onNavigate,
  variant = 'desktop',
}: SidebarProps) {
  const isOverlay = variant === 'overlay'
  const { user, signOut } = useAuth()
  const [leaving, setLeaving] = useState(false)

  const email = user?.email ?? ''
  const displayName =
    typeof user?.user_metadata?.full_name === 'string'
      ? user.user_metadata.full_name
      : email

  const handleSignOut = async () => {
    setLeaving(true)
    await signOut()
  }

  return (
    <div
      className={cn(
        'border-line bg-surface flex h-full flex-col border-r transition-[width] duration-200 ease-out',
        collapsed ? 'w-16' : 'w-60',
      )}
    >
      <div
        className={cn(
          'border-line flex h-16 shrink-0 items-center border-b',
          collapsed ? 'justify-center px-2' : 'justify-between px-4',
        )}
      >
        <Link
          to="/dashboard"
          onClick={onNavigate}
          className="flex items-center gap-2.5 overflow-hidden"
          aria-label={BRAND.name}
        >
          <LogoMark className="shrink-0" />
          {!collapsed && (
            <span className="text-[15px] font-semibold tracking-[-0.02em] whitespace-nowrap">
              {BRAND.name}
            </span>
          )}
        </Link>

        {!collapsed && (
          <button
            type="button"
            onClick={onToggleCollapsed}
            aria-label={isOverlay ? 'Închide meniul' : 'Restrânge meniul'}
            className={cn(
              'text-muted hover:text-fg hover:bg-hover rounded-btn size-7 place-items-center transition-colors',
              isOverlay ? 'grid' : 'hidden lg:grid',
            )}
          >
            {isOverlay ? (
              <X aria-hidden className="size-4" />
            ) : (
              <ChevronLeft aria-hidden className="size-4" />
            )}
          </button>
        )}
      </div>

      {collapsed && (
        <button
          type="button"
          onClick={onToggleCollapsed}
          aria-label="Extinde meniul"
          className="text-muted hover:text-fg hover:bg-hover mx-auto mt-3 hidden size-8 place-items-center rounded-btn transition-colors lg:grid"
        >
          <ChevronRight aria-hidden className="size-4" />
        </button>
      )}

      <nav className="flex-1 overflow-y-auto py-4">
        {navItems.map((item, index) => {
          const previous = navItems[index - 1]
          const startsNewGroup = previous && previous.group !== item.group

          return (
            <div key={item.to}>
              {startsNewGroup && <div className="bg-line mx-3 my-3 h-px" />}
              <NavLink
                to={item.to}
                end={item.to === '/dashboard'}
                onClick={onNavigate}
                title={collapsed ? item.label : undefined}
                className={({ isActive }) =>
                  cn(
                    // Border-left de 2px pe tot itemul; cel transparent tine alinierea
                    'mx-2 flex items-center gap-3 border-l-2 py-2.5 text-[13.5px] transition-colors',
                    collapsed ? 'justify-center px-2' : 'px-3',
                    isActive
                      ? 'border-l-amber bg-raised text-fg rounded-r-btn'
                      : 'hover:bg-hover hover:text-fg rounded-btn border-l-transparent text-[#666666]',
                  )
                }
              >
                <item.icon aria-hidden className="size-4 shrink-0" />
                {!collapsed && (
                  <span className="whitespace-nowrap">{item.label}</span>
                )}
              </NavLink>
            </div>
          )
        })}
      </nav>

      <div
        className={cn(
          'border-line shrink-0 border-t',
          collapsed ? 'px-2 py-3' : 'p-3',
        )}
      >
        <div
          className={cn(
            'flex items-center gap-2.5',
            collapsed && 'flex-col gap-2',
          )}
        >
          <Avatar name={displayName || 'A'} />
          {!collapsed && (
            <span className="min-w-0 flex-1">
              <span className="text-muted block truncate font-mono text-[10.5px]">
                {email}
              </span>
            </span>
          )}
          <button
            type="button"
            onClick={handleSignOut}
            disabled={leaving}
            aria-label="Deconectează-te"
            title="Deconectează-te"
            className="text-muted hover:text-fg hover:bg-hover rounded-btn grid size-8 shrink-0 place-items-center transition-colors disabled:opacity-50"
          >
            {leaving ? (
              <Spinner className="size-3.5" />
            ) : (
              <LogOut aria-hidden className="size-4" />
            )}
          </button>
        </div>
      </div>
    </div>
  )
}

import { Bell, LogOut, Menu, Settings, User } from 'lucide-react'
import { Link, useLocation } from 'react-router-dom'
import { titleForPath } from '@/components/layout/nav-items'
import { Avatar } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { useAuth } from '@/hooks/useAuth'
import { useProfile } from '@/lib/queries/useSettings'

export function TopBar({ onOpenMenu }: { onOpenMenu: () => void }) {
  const { user, signOut } = useAuth()
  const { pathname } = useLocation()
  // Planul vine din profil, nu dintr-o constanta: se schimba la upgrade
  const { data: profile } = useProfile()

  const email = user?.email ?? ''
  const displayName =
    typeof user?.user_metadata?.full_name === 'string'
      ? user.user_metadata.full_name
      : email

  return (
    <header className="border-line bg-ink/80 sticky top-0 z-30 border-b backdrop-blur-xl">
      <div className="flex h-16 items-center justify-between gap-4 px-4 sm:px-6">
        <div className="flex min-w-0 items-center gap-3">
          <button
            type="button"
            onClick={onOpenMenu}
            aria-label="Deschide meniul"
            className="text-muted hover:text-fg hover:bg-hover rounded-btn grid size-9 place-items-center transition-colors lg:hidden"
          >
            <Menu aria-hidden className="size-4" />
          </button>
          <h1 className="truncate text-[17px] font-medium tracking-[-0.02em]">
            {titleForPath(pathname)}
          </h1>
        </div>

        <div className="flex shrink-0 items-center gap-2 sm:gap-3">
          {profile && (
            <Link to="/dashboard/settings" className="hidden sm:block">
              <Badge tone="amber" className="capitalize">
                {profile.plan}
              </Badge>
            </Link>
          )}

          {/* Fara badge rosu: nu exista inca sursa reala de notificari */}
          <button
            type="button"
            aria-label="Notificări"
            title="Notificările vor apărea aici"
            className="text-muted hover:text-fg hover:bg-hover rounded-btn grid size-9 place-items-center transition-colors"
          >
            <Bell aria-hidden className="size-4" />
          </button>

          <DropdownMenu>
            <DropdownMenuTrigger
              aria-label="Meniu utilizator"
              className="hover:ring-line-strong rounded-full ring-1 ring-transparent transition-[box-shadow]"
            >
              <Avatar name={displayName || 'A'} />
            </DropdownMenuTrigger>
            <DropdownMenuContent>
              <DropdownMenuLabel>
                <span className="block truncate">{displayName}</span>
                <span className="text-muted mt-0.5 block truncate font-mono text-[10.5px] font-normal">
                  {email}
                </span>
              </DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem asChild>
                <Link to="/dashboard/settings">
                  <User aria-hidden />
                  Profil
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link to="/dashboard/settings">
                  <Settings aria-hidden />
                  Setări
                </Link>
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem tone="danger" onSelect={() => void signOut()}>
                <LogOut aria-hidden />
                Deconectează-te
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>
    </header>
  )
}

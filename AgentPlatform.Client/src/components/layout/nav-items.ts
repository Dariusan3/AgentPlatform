import {
  Bot,
  Building2,
  LayoutDashboard,
  MessageSquare,
  Settings,
  Users,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

export type NavItem = {
  to: string
  label: string
  icon: LucideIcon
  /** Ultimul grup e despartit de o linie */
  group: 'main' | 'system'
}

export const navItems: NavItem[] = [
  { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard, group: 'main' },
  {
    to: '/dashboard/properties',
    label: 'Proprietăți',
    icon: Building2,
    group: 'main',
  },
  { to: '/dashboard/agents', label: 'Agenți AI', icon: Bot, group: 'main' },
  { to: '/dashboard/leads', label: 'Leaduri', icon: Users, group: 'main' },
  {
    to: '/dashboard/conversations',
    label: 'Conversații',
    icon: MessageSquare,
    group: 'main',
  },
  { to: '/dashboard/settings', label: 'Setări', icon: Settings, group: 'system' },
]

/** Titlul din TopBar: cea mai specifica ruta care se potriveste */
export function titleForPath(pathname: string) {
  const match = [...navItems]
    .sort((a, b) => b.to.length - a.to.length)
    .find((item) => pathname === item.to || pathname.startsWith(`${item.to}/`))

  return match?.label ?? 'Dashboard'
}

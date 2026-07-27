import { useNavigate } from 'react-router-dom'
import { Bell, CheckCheck } from 'lucide-react'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { formatRelativeTime } from '@/lib/labels'
import {
  useMarkAllNotificationsRead,
  useMarkNotificationRead,
  useNotifications,
} from '@/lib/queries/useNotifications'
import { cn } from '@/lib/utils'
import type { AppNotification, NotificationSeverity } from '@/lib/types'

const severityDot: Record<NotificationSeverity, string> = {
  info: 'bg-muted',
  success: 'bg-success',
  warning: 'bg-accent',
  error: 'bg-danger',
}

/**
 * Clopotelul din bara de sus: numarul de necitite si ultimele notificari.
 */
export function NotificationsMenu() {
  const navigate = useNavigate()
  const { data } = useNotifications()
  const markRead = useMarkNotificationRead()
  const markAllRead = useMarkAllNotificationsRead()

  const unread = data?.unreadCount ?? 0
  const items = data?.items ?? []

  const open = (notification: AppNotification) => {
    if (!notification.read) markRead.mutate(notification.id)
    if (notification.link) navigate(notification.link)
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        aria-label={
          unread > 0 ? `Notificări, ${unread} necitite` : 'Notificări'
        }
        className="text-muted hover:text-fg hover:bg-hover rounded-btn relative grid size-9 place-items-center transition-colors"
      >
        <Bell aria-hidden className="size-4" />
        {unread > 0 && (
          <span
            aria-hidden
            className="bg-accent text-bg tnum absolute top-1 right-1 grid h-4 min-w-4 place-items-center rounded-full px-1 text-[9.5px] font-semibold"
          >
            {unread > 9 ? '9+' : unread}
          </span>
        )}
      </DropdownMenuTrigger>

      <DropdownMenuContent className="w-[min(22rem,calc(100vw-2rem))] p-0">
        <div className="border-line flex items-center justify-between border-b px-4 py-3">
          <span className="text-[13px] font-medium">Notificări</span>
          {unread > 0 && (
            <button
              type="button"
              onClick={() => markAllRead.mutate()}
              disabled={markAllRead.isPending}
              className="text-muted hover:text-fg flex items-center gap-1.5 text-[12px] transition-colors disabled:opacity-50"
            >
              <CheckCheck aria-hidden className="size-3.5" />
              Marchează toate
            </button>
          )}
        </div>

        {items.length === 0 ? (
          <p className="text-muted px-4 py-8 text-center text-[13px] leading-relaxed">
            Nicio notificare încă.
            <br />
            Aici ajung leadurile noi, apelurile și erorile.
          </p>
        ) : (
          <div className="max-h-[26rem] overflow-y-auto">
            {items.map((notification) => (
              <button
                key={notification.id}
                type="button"
                onClick={() => open(notification)}
                className={cn(
                  'border-line hover:bg-hover flex w-full gap-3 border-b px-4 py-3 text-left transition-colors last:border-b-0',
                  !notification.read && 'bg-hover/40',
                )}
              >
                <span
                  aria-hidden
                  className={cn(
                    'mt-1.5 size-1.5 shrink-0 rounded-full',
                    notification.read
                      ? 'bg-transparent'
                      : severityDot[notification.severity],
                  )}
                />

                <span className="min-w-0 flex-1">
                  <span className="flex items-baseline justify-between gap-3">
                    <span
                      className={cn(
                        'truncate text-[13px]',
                        notification.read ? 'text-muted' : 'font-medium',
                      )}
                    >
                      {notification.title}
                    </span>
                    <span className="text-muted shrink-0 font-mono text-[10.5px]">
                      {formatRelativeTime(notification.createdAt)}
                    </span>
                  </span>
                  <span className="text-muted mt-1 block text-[12.5px] leading-relaxed">
                    {notification.body}
                  </span>
                </span>
              </button>
            ))}
          </div>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

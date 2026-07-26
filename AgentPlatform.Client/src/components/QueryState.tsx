import type { ReactNode } from 'react'
import { AlertTriangle, RefreshCw } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Spinner } from '@/components/Spinner'
import { apiErrorMessage } from '@/lib/api'

/** Scheletul afisat cat se incarca datele. Aceeasi inaltime ca si continutul. */
export function LoadingCard({ label = 'Se încarcă…' }: { label?: string }) {
  return (
    <Card className="grid place-items-center px-6 py-16 text-center">
      <Spinner className="size-5" />
      <p className="text-muted mt-4 text-[13.5px]">{label}</p>
    </Card>
  )
}

/**
 * Erorile spun ce s-a intamplat si ofera reincercarea. Un ecran gol care nu
 * explica nimic e cea mai proasta stare posibila.
 */
export function ErrorCard({
  error,
  onRetry,
}: {
  error: unknown
  onRetry?: () => void
}) {
  return (
    <Card className="grid place-items-center px-6 py-14 text-center">
      <AlertTriangle aria-hidden className="text-danger size-5" />
      <p className="mt-4 text-[15px] font-medium">Nu am putut încărca datele</p>
      <p className="text-muted mt-2 max-w-md text-[13.5px] leading-relaxed">
        {apiErrorMessage(error)}
      </p>
      {onRetry && (
        <Button variant="outline" size="sm" className="mt-5" onClick={onRetry}>
          <RefreshCw aria-hidden className="size-3.5" />
          Încearcă din nou
        </Button>
      )}
    </Card>
  )
}

export function EmptyCard({
  icon,
  title,
  description,
  action,
}: {
  icon: ReactNode
  title: string
  description: string
  action?: ReactNode
}) {
  return (
    <Card className="grid place-items-center px-6 py-16 text-center">
      <span className="text-muted">{icon}</span>
      <p className="mt-4 text-[15px] font-medium">{title}</p>
      <p className="text-muted mt-2 max-w-sm text-[13.5px] leading-relaxed">
        {description}
      </p>
      {action && <div className="mt-6">{action}</div>}
    </Card>
  )
}

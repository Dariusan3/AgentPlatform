import { cn } from '@/lib/utils'

export function Spinner({ className }: { className?: string }) {
  return (
    <span
      role="status"
      aria-label="Se încarcă"
      className={cn(
        'border-fg/20 border-t-amber inline-block size-4 animate-spin rounded-full border-2',
        className,
      )}
    />
  )
}

/** Ecran plin, folosit cat timp nu stim inca daca exista sesiune */
export function FullPageSpinner() {
  return (
    <div className="bg-ink grid min-h-svh place-items-center">
      <Spinner className="size-6" />
    </div>
  )
}

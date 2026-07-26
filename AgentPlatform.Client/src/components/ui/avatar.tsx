import { cn } from '@/lib/utils'

/**
 * Culoarea vine din numele agentului, deterministic: acelasi agent are mereu
 * aceeasi culoare, fara sa o stocam nicaieri.
 */
const palettes = [
  'bg-amber/12 text-amber',
  'bg-info/12 text-info',
  'bg-violet/12 text-violet',
  'bg-success/12 text-success',
]

export function initials(name: string) {
  return name
    .replace(/[^\p{L}\s-]/gu, '')
    .split(/[\s-]+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((word) => word[0]?.toUpperCase() ?? '')
    .join('')
}

export function paletteFor(seed: string) {
  let hash = 0
  for (const char of seed) hash = (hash * 31 + char.charCodeAt(0)) % 997
  return palettes[hash % palettes.length]
}

export function Avatar({
  name,
  className,
  tinted,
}: {
  name: string
  className?: string
  /** Coloreaza pe baza numelui. Fara el, avatarul e neutru. */
  tinted?: boolean
}) {
  return (
    <span
      aria-hidden
      className={cn(
        'grid shrink-0 place-items-center rounded-full font-medium',
        tinted ? paletteFor(name) : 'bg-raised text-fg',
        'size-8 text-[11px]',
        className,
      )}
    >
      {initials(name)}
    </span>
  )
}

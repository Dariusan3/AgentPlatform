import { BRAND } from '@/lib/brand'
import { cn } from '@/lib/utils'

/**
 * Semnul: o arcada (usa) cu un punct amber in dreapta, adica vizorul.
 * Portarul e la intrare si e treaz - punctul e singurul element colorat.
 */
export function LogoMark({ className }: { className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      aria-hidden
      className={cn('size-6', className)}
    >
      <path
        d="M4.5 21V9.5a7.5 7.5 0 0 1 15 0V21"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinecap="round"
      />
      <path d="M2.5 21h19" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" />
      <circle cx="15.25" cy="12.5" r="1.4" fill="var(--color-amber)" />
    </svg>
  )
}

export function Logo({ className }: { className?: string }) {
  return (
    <span className={cn('inline-flex items-center gap-2.5', className)}>
      <LogoMark />
      <span className="text-[17px] font-semibold tracking-[-0.02em]">
        {BRAND.name}
      </span>
    </span>
  )
}

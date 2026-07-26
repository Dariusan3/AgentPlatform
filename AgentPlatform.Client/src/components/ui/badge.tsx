import * as React from 'react'
import { cn } from '@/lib/utils'

export type BadgeTone =
  | 'success'
  | 'muted'
  | 'amber'
  | 'info'
  | 'danger'
  | 'outline'

/**
 * Fundalurile foarte inchise (#052e16, #1a0a00) tin culoarea la nivel de
 * accent: textul e cel care poarta semnalul, nu suprafata.
 */
const tones: Record<BadgeTone, string> = {
  success: 'border-[#166534] bg-[#052e16] text-success',
  muted: 'border-transparent bg-raised text-muted',
  amber: 'border-[#92400e] bg-[#1a0a00] text-amber',
  info: 'border-[#1e3a5f] bg-[#0a1628] text-info',
  danger: 'border-[#7f1d1d] bg-[#1a0505] text-danger',
  outline: 'border-line-strong bg-transparent text-muted',
}

type BadgeProps = React.ComponentPropsWithoutRef<'span'> & {
  tone?: BadgeTone
}

export function Badge({ className, tone = 'muted', ...props }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex shrink-0 items-center gap-1.5 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-widest uppercase',
        tones[tone],
        className,
      )}
      {...props}
    />
  )
}

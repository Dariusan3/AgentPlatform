import { cn } from '@/lib/utils'

export type ButtonVariant = 'primary' | 'outline' | 'ghost'
export type ButtonSize = 'sm' | 'md' | 'lg'

const base =
  'inline-flex items-center justify-center gap-2 rounded-btn font-medium whitespace-nowrap transition-[background-color,border-color,color,opacity] duration-200 disabled:pointer-events-none disabled:opacity-50'

const variants: Record<ButtonVariant, string> = {
  // Singurul element plin din pagina - de aceea CTA-ul principal nu se pierde
  primary: 'bg-amber text-ink hover:bg-amber/90',
  outline:
    'border border-line-strong text-fg hover:border-fg/40 hover:bg-fg/[0.04]',
  ghost: 'text-muted hover:text-fg',
}

const sizes: Record<ButtonSize, string> = {
  sm: 'h-9 px-3.5 text-[13px]',
  md: 'h-10 px-4 text-sm',
  lg: 'h-12 px-6 text-[15px]',
}

/** Separat de button.tsx ca fast refresh sa nu se rupa pe export mixt */
export function buttonClasses({
  variant = 'primary',
  size = 'md',
  className,
}: {
  variant?: ButtonVariant
  size?: ButtonSize
  className?: string
} = {}) {
  return cn(base, variants[variant], sizes[size], className)
}

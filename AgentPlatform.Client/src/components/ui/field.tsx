import * as React from 'react'
import { ChevronDown } from 'lucide-react'
import { cn } from '@/lib/utils'

const control =
  'w-full rounded-btn border border-line-strong bg-ink px-3.5 text-[14px] text-fg placeholder:text-muted/70 transition-colors hover:border-fg/25 focus:border-amber focus:outline-none disabled:opacity-50'

export const Input = React.forwardRef<
  HTMLInputElement,
  React.ComponentPropsWithoutRef<'input'>
>(({ className, ...props }, ref) => (
  <input ref={ref} className={cn(control, 'h-10', className)} {...props} />
))
Input.displayName = 'Input'

export const Textarea = React.forwardRef<
  HTMLTextAreaElement,
  React.ComponentPropsWithoutRef<'textarea'>
>(({ className, rows = 4, ...props }, ref) => (
  <textarea
    ref={ref}
    rows={rows}
    className={cn(control, 'resize-y py-2.5 leading-relaxed', className)}
    {...props}
  />
))
Textarea.displayName = 'Textarea'

/**
 * `select` nativ, nu Radix Select: pe mobil deschide picker-ul sistemului, care
 * e mai bun decat orice lista pe care am construi-o noi.
 */
export const Select = React.forwardRef<
  HTMLSelectElement,
  React.ComponentPropsWithoutRef<'select'>
>(({ className, children, ...props }, ref) => (
  <div className="relative">
    <select
      ref={ref}
      className={cn(control, 'h-10 cursor-pointer appearance-none pr-9', className)}
      {...props}
    >
      {children}
    </select>
    <ChevronDown
      aria-hidden
      className="text-muted pointer-events-none absolute top-1/2 right-3 size-3.5 -translate-y-1/2"
    />
  </div>
))
Select.displayName = 'Select'

export function Label({
  className,
  ...props
}: React.ComponentPropsWithoutRef<'label'>) {
  return (
    <label
      className={cn(
        'text-muted block font-mono text-[10.5px] tracking-widest uppercase',
        className,
      )}
      {...props}
    />
  )
}

/** Eticheta + control, cu spatierea folosita in toate formularele */
export function Field({
  label,
  htmlFor,
  hint,
  className,
  children,
}: {
  label: string
  htmlFor: string
  hint?: React.ReactNode
  className?: string
  children: React.ReactNode
}) {
  return (
    <div className={cn('space-y-2', className)}>
      <Label htmlFor={htmlFor}>{label}</Label>
      {children}
      {hint && <p className="text-muted text-[12px] leading-relaxed">{hint}</p>}
    </div>
  )
}

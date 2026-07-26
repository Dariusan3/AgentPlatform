import * as React from 'react'
import { cn } from '@/lib/utils'

export function Card({
  className,
  ...props
}: React.ComponentPropsWithoutRef<'div'>) {
  return (
    <div
      className={cn('border-line bg-surface rounded-card border', className)}
      {...props}
    />
  )
}

export function CardHeader({
  className,
  ...props
}: React.ComponentPropsWithoutRef<'div'>) {
  return (
    <div
      className={cn(
        'border-line flex items-center justify-between gap-4 border-b px-5 py-4',
        className,
      )}
      {...props}
    />
  )
}

export function CardTitle({
  className,
  ...props
}: React.ComponentPropsWithoutRef<'h2'>) {
  return (
    <h2
      className={cn('text-[15px] font-medium tracking-[-0.01em]', className)}
      {...props}
    />
  )
}

export function CardBody({
  className,
  ...props
}: React.ComponentPropsWithoutRef<'div'>) {
  return <div className={cn('p-5', className)} {...props} />
}

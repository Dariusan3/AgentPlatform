import * as React from 'react'
import * as TabsPrimitive from '@radix-ui/react-tabs'
import { cn } from '@/lib/utils'

export const Tabs = TabsPrimitive.Root

/**
 * Linia de baza sta pe wrapper, nu pe lista: lista are overflow-x-auto pentru
 * derulare pe mobil, iar orice element desenat in afara casetei ei ar fi tăiat.
 * De aceea indicatorul tabului activ e un border al triggerului, nu un ::after.
 */
export const TabsList = React.forwardRef<
  React.ComponentRef<typeof TabsPrimitive.List>,
  React.ComponentPropsWithoutRef<typeof TabsPrimitive.List>
>(({ className, ...props }, ref) => (
  <div className={cn('border-line border-b', className)}>
    <TabsPrimitive.List
      ref={ref}
      className="-mb-px flex items-center gap-1 overflow-x-auto"
      {...props}
    />
  </div>
))
TabsList.displayName = 'TabsList'

export const TabsTrigger = React.forwardRef<
  React.ComponentRef<typeof TabsPrimitive.Trigger>,
  React.ComponentPropsWithoutRef<typeof TabsPrimitive.Trigger>
>(({ className, ...props }, ref) => (
  <TabsPrimitive.Trigger
    ref={ref}
    className={cn(
      'text-muted hover:text-fg data-[state=active]:text-fg data-[state=active]:border-b-amber flex shrink-0 items-center gap-2 border-b-2 border-transparent px-3.5 py-2.5 text-[13.5px] whitespace-nowrap transition-colors',
      // Inelul de focus ar fi si el tăiat, deci marcam focalizarea cu fundal
      'focus-visible:bg-hover focus-visible:outline-none',
      className,
    )}
    {...props}
  />
))
TabsTrigger.displayName = 'TabsTrigger'

export const TabsContent = React.forwardRef<
  React.ComponentRef<typeof TabsPrimitive.Content>,
  React.ComponentPropsWithoutRef<typeof TabsPrimitive.Content>
>(({ className, ...props }, ref) => (
  <TabsPrimitive.Content ref={ref} className={cn('pt-8', className)} {...props} />
))
TabsContent.displayName = 'TabsContent'

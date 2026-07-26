import * as React from 'react'
import { buttonClasses } from '@/components/ui/button-variants'
import type { ButtonSize, ButtonVariant } from '@/components/ui/button-variants'

type ButtonProps = React.ComponentPropsWithoutRef<'button'> & {
  variant?: ButtonVariant
  size?: ButtonSize
}

export const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, type = 'button', ...props }, ref) => (
    <button
      ref={ref}
      type={type}
      className={buttonClasses({ variant, size, className })}
      {...props}
    />
  ),
)
Button.displayName = 'Button'

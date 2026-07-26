import { useId, useState } from 'react'
import type { ReactNode } from 'react'
import { Eye, EyeOff } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Logo } from '@/components/Logo'
import { PasswordMeter } from '@/components/auth/PasswordMeter'
import { authInputClasses } from '@/components/auth/auth-styles'
import { cn } from '@/lib/utils'

type AuthShellProps = {
  title: string
  subtitle: string
  children: ReactNode
  /** Linia de sub card: trimite catre celalalt formular */
  footer: ReactNode
}

export function AuthShell({
  title,
  subtitle,
  children,
  footer,
}: AuthShellProps) {
  return (
    <main className="relative isolate grid min-h-svh place-items-center px-5 py-14">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 -z-10 h-[460px]"
      >
        <div className="animate-breathe absolute top-[-240px] left-1/2 size-[700px] -translate-x-1/2 rounded-full bg-[radial-gradient(circle,rgba(245,166,35,0.16),transparent_60%)]" />
      </div>

      <div className="relative w-full max-w-[420px]">
        <Link
          to="/"
          className="mx-auto flex w-fit"
          aria-label="Înapoi la pagina principală"
        >
          <Logo />
        </Link>

        <div className="border-line bg-surface rounded-card mt-8 border p-6 sm:p-8">
          <h1 className="text-[24px] font-medium tracking-[-0.03em]">{title}</h1>
          <p className="text-muted mt-2 text-[13.5px]">{subtitle}</p>
          {children}
        </div>

        <p className="text-muted mt-6 text-center text-[13.5px]">{footer}</p>
      </div>
    </main>
  )
}

type AuthFieldProps = {
  id: string
  label: string
  type: string
  placeholder: string
  autoComplete: string
  /** Ex. link-ul "Ai uitat parola?", aliniat in dreapta etichetei */
  aside?: ReactNode
  hint?: string
  /** Marcheaza campul care a picat validarea, ca mesajul sa nu fie doar in toast */
  invalid?: boolean
  /** Afiseaza indicatorul de putere sub camp (doar la parola nouă) */
  showStrength?: boolean
}

export function AuthField({
  id,
  label,
  type,
  placeholder,
  autoComplete,
  aside,
  hint,
  invalid,
  showStrength,
}: AuthFieldProps) {
  const isPassword = type === 'password'
  const [revealed, setRevealed] = useState(false)
  const [value, setValue] = useState('')
  const hintId = useId()

  return (
    <div className="space-y-2">
      <div className="flex items-baseline justify-between gap-4">
        <label
          htmlFor={id}
          className="text-muted block font-mono text-[10.5px] tracking-widest uppercase"
        >
          {label}
        </label>
        {aside}
      </div>

      <div className="relative">
        <input
          id={id}
          name={id}
          type={isPassword && revealed ? 'text' : type}
          autoComplete={autoComplete}
          placeholder={placeholder}
          value={value}
          onChange={(event) => setValue(event.target.value)}
          aria-invalid={invalid || undefined}
          aria-describedby={hint ? hintId : undefined}
          className={cn(
            authInputClasses,
            isPassword && 'pr-11',
            invalid && 'border-red-500/60 hover:border-red-500/60',
          )}
        />

        {isPassword && (
          <button
            type="button"
            onClick={() => setRevealed((shown) => !shown)}
            aria-label={revealed ? 'Ascunde parola' : 'Arată parola'}
            aria-pressed={revealed}
            className="text-muted hover:text-fg absolute inset-y-0 right-0 grid w-11 place-items-center transition-colors"
          >
            {revealed ? (
              <EyeOff aria-hidden className="size-4" />
            ) : (
              <Eye aria-hidden className="size-4" />
            )}
          </button>
        )}
      </div>

      {showStrength && isPassword && <PasswordMeter password={value} />}

      {hint && !showStrength && (
        <p id={hintId} className="text-muted text-[12px]">
          {hint}
        </p>
      )}
    </div>
  )
}

export function GoogleMark() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden className="size-4">
      <path
        fill="currentColor"
        d="M21.35 11.1H12v2.9h5.35c-.25 1.5-1.8 4.4-5.35 4.4A6.4 6.4 0 0 1 12 5.2c1.65 0 2.95.65 3.85 1.5l2.1-2.05A9.2 9.2 0 0 0 12 2.1a9.9 9.9 0 0 0 0 19.8c5.7 0 9.5-4 9.5-9.65 0-.4-.05-.75-.15-1.15Z"
      />
    </svg>
  )
}

export function AuthDivider() {
  return (
    <div className="my-6 flex items-center gap-4">
      <span className="bg-line h-px flex-1" />
      <span className="text-muted font-mono text-[10.5px] tracking-widest uppercase">
        sau
      </span>
      <span className="bg-line h-px flex-1" />
    </div>
  )
}

import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { MailCheck } from 'lucide-react'
import { toast } from 'sonner'
import { AuthDivider, AuthField, AuthShell } from '@/components/auth/AuthShell'
import { GoogleButton } from '@/components/auth/GoogleButton'
import { Spinner } from '@/components/Spinner'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/hooks/useAuth'
import { BRAND } from '@/lib/brand'
import { MIN_PASSWORD } from '@/lib/passwordStrength'

type Problem = { field: string; message: string }

/** Validare in ordinea in care utilizatorul completeaza formularul */
function validate(fields: {
  fullName: string
  email: string
  password: string
  confirmPassword: string
}): Problem | null {
  if (fields.fullName.trim().length < 2) {
    return { field: 'fullName', message: 'Scrie-ți numele complet.' }
  }
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(fields.email.trim())) {
    return { field: 'email', message: 'Adresa de email nu pare validă.' }
  }
  if (fields.password.length < MIN_PASSWORD) {
    return {
      field: 'password',
      message: `Parola trebuie să aibă minim ${MIN_PASSWORD} caractere.`,
    }
  }
  if (fields.password !== fields.confirmPassword) {
    return { field: 'confirmPassword', message: 'Parolele nu se potrivesc.' }
  }
  return null
}

/** Un singur toast de auth pe ecran: retrimiterea il inlocuieste, nu il stivuieste */
const TOAST_ID = 'auth'

export function SignupPage() {
  const { signUp } = useAuth()
  const [busy, setBusy] = useState(false)
  const [invalidField, setInvalidField] = useState<string | null>(null)
  const [sentTo, setSentTo] = useState<string | null>(null)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (busy) return

    const form = new FormData(event.currentTarget)
    const fields = {
      fullName: String(form.get('fullName') ?? ''),
      email: String(form.get('email') ?? ''),
      password: String(form.get('password') ?? ''),
      confirmPassword: String(form.get('confirmPassword') ?? ''),
    }

    const problem = validate(fields)
    if (problem) {
      setInvalidField(problem.field)
      toast.error(problem.message, { id: TOAST_ID })
      return
    }

    setInvalidField(null)
    setBusy(true)

    const result = await signUp(fields.email, fields.password, fields.fullName)

    if (result.error) {
      setInvalidField('email')
      toast.error(result.error, { id: TOAST_ID })
      setBusy(false)
      return
    }

    // Cu confirmarea activa nu exista sesiune, deci nu are unde sa fie redirectat.
    // Fara confirmare, onAuthStateChange il duce singur pe /dashboard.
    if (result.needsConfirmation) {
      setSentTo(fields.email.trim())
    } else {
      toast.success('Contul e gata. Bine ai venit!', { id: TOAST_ID })
    }
    setBusy(false)
  }

  if (sentTo) {
    return (
      <AuthShell
        title="Verifică emailul tău"
        subtitle="Mai e un pas și contul e gata."
        footer={
          <>
            Ai confirmat deja?{' '}
            <Link
              to="/login"
              className="text-amber underline decoration-amber/30 underline-offset-4 transition-colors hover:decoration-amber"
            >
              Conectează-te
            </Link>
          </>
        }
      >
        <div className="mt-7 flex gap-3.5">
          <MailCheck aria-hidden className="text-amber mt-0.5 size-5 shrink-0" />
          <div className="space-y-3 text-[13.5px] leading-relaxed">
            <p>
              Am trimis un link de confirmare la{' '}
              <span className="font-medium">{sentTo}</span>. Deschide-l și te
              aducem direct în cont.
            </p>
            <p className="text-muted">
              Nu a ajuns în două minute? Verifică folderul de spam — sau
              încearcă din nou cu altă adresă.
            </p>
          </div>
        </div>

        <Button
          variant="outline"
          size="lg"
          className="mt-7 w-full"
          onClick={() => setSentTo(null)}
        >
          Folosește altă adresă
        </Button>
      </AuthShell>
    )
  }

  return (
    <AuthShell
      title="Creează-ți contul"
      subtitle={`${BRAND.trialDays} zile gratuit, fără card bancar.`}
      footer={
        <>
          Ai deja cont?{' '}
          <Link
            to="/login"
            className="text-amber underline decoration-amber/30 underline-offset-4 transition-colors hover:decoration-amber"
          >
            Conectează-te
          </Link>
        </>
      }
    >
      <form onSubmit={handleSubmit} noValidate className="mt-7 space-y-4">
        <AuthField
          id="fullName"
          label="Nume complet"
          type="text"
          autoComplete="name"
          placeholder="Maria Ionescu"
          invalid={invalidField === 'fullName'}
        />

        <AuthField
          id="email"
          label="Email"
          type="email"
          autoComplete="email"
          placeholder="maria@agentia.ro"
          invalid={invalidField === 'email'}
        />

        <AuthField
          id="password"
          label="Parolă"
          type="password"
          autoComplete="new-password"
          placeholder="••••••••"
          invalid={invalidField === 'password'}
          showStrength
        />

        <AuthField
          id="confirmPassword"
          label="Confirmă parola"
          type="password"
          autoComplete="new-password"
          placeholder="••••••••"
          invalid={invalidField === 'confirmPassword'}
        />

        <Button type="submit" size="lg" className="w-full" disabled={busy}>
          {busy && <Spinner className="size-4" />}
          {busy ? 'Se creează contul…' : 'Creează cont'}
        </Button>
      </form>

      <AuthDivider />

      <GoogleButton
        label="Continuă cu Google"
        onError={(message) => toast.error(message, { id: TOAST_ID })}
      />
    </AuthShell>
  )
}

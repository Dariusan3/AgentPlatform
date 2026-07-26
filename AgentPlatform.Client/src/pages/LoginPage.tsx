import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import {
  AuthDivider,
  AuthField,
  AuthShell,
} from '@/components/auth/AuthShell'
import { GoogleButton } from '@/components/auth/GoogleButton'
import { Spinner } from '@/components/Spinner'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/hooks/useAuth'

/** Un singur toast de auth pe ecran: retrimiterea il inlocuieste, nu il stivuieste */
const TOAST_ID = 'auth'

export function LoginPage() {
  const { signIn } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [busy, setBusy] = useState(false)
  // Toastul dispare; conturul rosu de pe camp ramane, ca sa nu se piarda semnalul
  const [invalidField, setInvalidField] = useState<string | null>(null)

  // Daca a fost trimis aici de ProtectedRoute, il ducem inapoi unde voia
  const from =
    typeof (location.state as { from?: string } | null)?.from === 'string'
      ? (location.state as { from: string }).from
      : '/dashboard'

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (busy) return

    const form = new FormData(event.currentTarget)
    const email = String(form.get('email') ?? '')
    const password = String(form.get('password') ?? '')

    if (!email) {
      setInvalidField('email')
      toast.error('Completează emailul.', { id: TOAST_ID })
      return
    }
    if (!password) {
      setInvalidField('password')
      toast.error('Completează parola.', { id: TOAST_ID })
      return
    }

    setInvalidField(null)
    setBusy(true)

    const result = await signIn(email, password)

    if (result.error) {
      setInvalidField('password')
      toast.error(result.error, {
        id: TOAST_ID,
        description: 'Verifică datele sau resetează parola.',
      })
      setBusy(false)
      return
    }

    toast.success('Bine ai revenit!', { id: TOAST_ID })
    navigate(from, { replace: true })
  }

  return (
    <AuthShell
      title="Bine ai revenit"
      subtitle="Continuă unde ai rămas cu leadurile tale."
      footer={
        <>
          Nu ai cont?{' '}
          <Link
            to="/signup"
            className="text-amber underline decoration-amber/30 underline-offset-4 transition-colors hover:decoration-amber"
          >
            Creează cont
          </Link>
        </>
      }
    >
      <form onSubmit={handleSubmit} noValidate className="mt-7 space-y-4">
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
          autoComplete="current-password"
          placeholder="••••••••"
          invalid={invalidField === 'password'}
          aside={
            <a
              href="#"
              className="text-muted hover:text-amber text-[12px] transition-colors"
            >
              Ai uitat parola?
            </a>
          }
        />

        <Button type="submit" size="lg" className="w-full" disabled={busy}>
          {busy && <Spinner className="size-4" />}
          {busy ? 'Se conectează…' : 'Conectează-te'}
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

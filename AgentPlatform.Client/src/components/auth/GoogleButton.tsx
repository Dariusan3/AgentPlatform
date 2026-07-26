import { useState } from 'react'
import { GoogleMark } from '@/components/auth/AuthShell'
import { Spinner } from '@/components/Spinner'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/hooks/useAuth'

/**
 * La succes browserul pleaca spre Google, deci starea `busy` nu se mai reseteaza.
 * O resetam doar pe eroare, cand chiar ramanem in pagina.
 */
export function GoogleButton({
  label,
  onError,
}: {
  label: string
  onError: (message: string) => void
}) {
  const { signInWithGoogle, googleEnabled } = useAuth()
  const [busy, setBusy] = useState(false)

  const handleClick = async () => {
    setBusy(true)
    const { error } = await signInWithGoogle()
    if (error) {
      onError(error)
      setBusy(false)
    }
  }

  // Cat timp Google nu e activat in Supabase, butonul se vede dar nu duce nicaieri
  if (!googleEnabled) {
    return (
      <>
        <Button variant="outline" size="lg" className="w-full" disabled>
          <GoogleMark />
          {label}
        </Button>
        <p className="text-muted mt-2.5 text-center text-[12px]">
          Momentan disponibil doar cu email și parolă.
        </p>
      </>
    )
  }

  return (
    <Button
      variant="outline"
      size="lg"
      className="w-full"
      onClick={handleClick}
      disabled={busy}
    >
      {busy ? <Spinner className="size-4" /> : <GoogleMark />}
      {label}
    </Button>
  )
}

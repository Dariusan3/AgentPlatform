import { AuthError } from '@supabase/supabase-js'

/**
 * Supabase raspunde in engleza. Traducem pe `code` unde exista (supabase-js 2.x)
 * si cadem pe potrivire de mesaj pentru erorile mai vechi, fara cod.
 */
const byCode: Record<string, string> = {
  invalid_credentials: 'Email sau parolă incorectă.',
  email_not_confirmed:
    'Trebuie să confirmi adresa de email înainte de a te conecta. Verifică inbox-ul.',
  email_exists: 'Există deja un cont cu acest email.',
  user_already_exists: 'Există deja un cont cu acest email.',
  weak_password: 'Parola e prea slabă. Folosește minim 6 caractere.',
  same_password: 'Parola nouă trebuie să fie diferită de cea veche.',
  over_request_rate_limit:
    'Prea multe încercări. Așteaptă un minut și încearcă din nou.',
  over_email_send_rate_limit:
    'Am trimis deja prea multe emailuri către această adresă. Încearcă mai târziu.',
  validation_failed: 'Verifică datele introduse.',
  signup_disabled: 'Înregistrările sunt momentan închise.',
  email_address_invalid: 'Adresa de email nu pare validă.',
  user_not_found: 'Nu există niciun cont cu acest email.',
}

/**
 * Verificate inaintea codului, fiindca sunt mai specifice decat el.
 * Ex.: provider dezactivat vine ca `validation_failed`, care altfel s-ar
 * traduce generic prin "Verifica datele introduse".
 */
const specificMessages: [RegExp, string][] = [
  [
    /provider is not enabled|unsupported provider/i,
    'Acest mod de autentificare nu e activat încă. Folosește email și parolă.',
  ],
  [
    /error sending confirmation|error sending email/i,
    'Contul a fost creat, dar nu am putut trimite emailul de confirmare. Scrie-ne la salut@portar.ro.',
  ],
]

const byMessage: [RegExp, string][] = [
  [/invalid login credentials/i, 'Email sau parolă incorectă.'],
  [
    /email not confirmed/i,
    'Trebuie să confirmi adresa de email înainte de a te conecta. Verifică inbox-ul.',
  ],
  [/already registered|already exists/i, 'Există deja un cont cu acest email.'],
  [
    /password should be at least/i,
    'Parola e prea scurtă. Folosește minim 6 caractere.',
  ],
  [/rate limit|too many requests/i, 'Prea multe încercări. Încearcă din nou în câteva minute.'],
  [/failed to fetch|network/i, 'Nu am putut contacta serverul. Verifică conexiunea.'],
]

export function translateAuthError(error: unknown): string {
  if (!(error instanceof Error)) {
    return 'Ceva nu a funcționat. Încearcă din nou.'
  }

  for (const [pattern, text] of specificMessages) {
    if (pattern.test(error.message)) return text
  }

  if (error instanceof AuthError && error.code && byCode[error.code]) {
    return byCode[error.code]
  }

  for (const [pattern, text] of byMessage) {
    if (pattern.test(error.message)) return text
  }

  return 'Ceva nu a funcționat. Încearcă din nou.'
}

export const MIN_PASSWORD = 6

/** Cate segmente are indicatorul */
export const STRENGTH_STEPS = 4

export type PasswordStrength = {
  /** 0 = prea scurta, pana la 4 = puternica */
  score: number
  label: string
  /** Ce ar imbunatati parola, ca sfatul sa fie concret */
  hint: string | null
}

/**
 * Nu masuram entropie reala, ci lucrurile pe care utilizatorul le poate
 * schimba imediat: lungime si varietate de caractere. Un indicator care spune
 * "slaba" fara sa spuna de ce nu ajuta pe nimeni.
 */
export function getPasswordStrength(password: string): PasswordStrength {
  if (!password) {
    return { score: 0, label: '', hint: null }
  }

  if (password.length < MIN_PASSWORD) {
    return {
      score: 0,
      label: 'prea scurtă',
      hint: `Încă ${MIN_PASSWORD - password.length} caractere până la minim.`,
    }
  }

  const hasLower = /[a-z]/.test(password)
  const hasUpper = /[A-Z]/.test(password)
  const hasDigit = /\d/.test(password)
  const hasSymbol = /[^A-Za-z0-9]/.test(password)
  const variety = [hasLower, hasUpper, hasDigit, hasSymbol].filter(Boolean).length

  let score = 1
  if (password.length >= 10) score++
  if (password.length >= 14) score++
  if (variety >= 3) score++
  score = Math.min(score, STRENGTH_STEPS)

  const labels = ['', 'slabă', 'acceptabilă', 'bună', 'puternică']

  let hint: string | null = null
  if (password.length < 10) {
    hint = 'Parolele lungi bat parolele complicate. Încearcă 12+ caractere.'
  } else if (variety < 3) {
    hint = 'Adaugă o majusculă, o cifră sau un simbol.'
  }

  return { score, label: labels[score], hint }
}

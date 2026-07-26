import {
  getPasswordStrength,
  STRENGTH_STEPS,
} from '@/lib/passwordStrength'
import { cn } from '@/lib/utils'

/**
 * Segmentele se umplu cu amber, iar judecata o duce eticheta. Am evitat
 * scara rosu-galben-verde: ar aduce doua culori noi intr-o paleta care are
 * un singur accent, iar rosul e rezervat erorilor.
 */
export function PasswordMeter({ password }: { password: string }) {
  const { score, label, hint } = getPasswordStrength(password)
  const tooShort = password.length > 0 && score === 0

  return (
    <div aria-live="polite" className="space-y-2 pt-1">
      <div className="flex items-center gap-3">
        <div className="flex flex-1 gap-1">
          {Array.from({ length: STRENGTH_STEPS }, (_, i) => (
            <span
              key={i}
              className={cn(
                'h-0.5 flex-1 rounded-full transition-colors duration-300',
                i < score
                  ? 'bg-amber'
                  : tooShort
                    ? 'bg-red-500/50'
                    : 'bg-line-strong',
              )}
            />
          ))}
        </div>
        {label && (
          <span
            className={cn(
              'w-[7.5rem] shrink-0 text-right font-mono text-[10.5px] tracking-wide',
              tooShort ? 'text-red-300' : 'text-muted',
            )}
          >
            {label}
          </span>
        )}
      </div>

      {hint && <p className="text-muted text-[12px] leading-relaxed">{hint}</p>}
    </div>
  )
}

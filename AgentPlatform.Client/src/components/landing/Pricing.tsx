import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Check } from 'lucide-react'
import { Reveal } from '@/components/Reveal'
import { buttonClasses } from '@/components/ui/button-variants'
import { cn } from '@/lib/utils'

type Plan = {
  name: string
  monthly: number
  blurb: string
  features: string[]
  popular?: boolean
}

const plans: Plan[] = [
  {
    name: 'Starter',
    monthly: 99,
    blurb: 'Pentru agentul care lucrează singur.',
    features: [
      '1 agent AI',
      '500 conversații pe lună',
      'WhatsApp inclus',
      'Suport pe email',
    ],
  },
  {
    name: 'Pro',
    monthly: 249,
    blurb: 'Pentru cei cu portofoliu în creștere.',
    popular: true,
    features: [
      '3 agenți AI',
      'Conversații nelimitate',
      'Multi-platform',
      'Scoring de leaduri',
      'Suport prioritar',
    ],
  },
  {
    name: 'Agency',
    monthly: 599,
    blurb: 'Pentru agenții cu echipă și brand propriu.',
    features: [
      'Agenți nelimitați',
      'CRM complet',
      'Analytics avansate',
      'White-label',
      'Suport dedicat',
    ],
  },
]

export function Pricing() {
  const [annual, setAnnual] = useState(false)

  return (
    <section
      id="pricing"
      className="border-line scroll-mt-16 border-t px-5 py-24 sm:px-8 sm:py-32"
    >
      <div className="mx-auto max-w-6xl">
        <Reveal className="flex flex-col items-start justify-between gap-8 md:flex-row md:items-end">
          <header className="max-w-xl">
            <p className="text-amber font-mono text-[11px] tracking-[0.14em] uppercase">
              prețuri
            </p>
            <h2 className="mt-4 text-[30px] leading-[1.1] font-medium tracking-[-0.035em] sm:text-[40px]">
              Simplu și transparent
            </h2>
            <p className="text-muted mt-4 text-[16px] leading-relaxed">
              Un abonament acoperă un singur lead recuperat. Restul e profit.
            </p>
          </header>

          <div
            role="group"
            aria-label="Perioadă de facturare"
            className="border-line bg-surface rounded-btn flex shrink-0 border p-1"
          >
            {[
              { label: 'Lunar', value: false },
              { label: 'Anual', value: true },
            ].map((option) => (
              <button
                key={option.label}
                type="button"
                onClick={() => setAnnual(option.value)}
                aria-pressed={annual === option.value}
                className={cn(
                  'rounded-[6px] px-3.5 py-1.5 text-[13px] transition-colors',
                  annual === option.value
                    ? 'bg-fg/[0.09] text-fg'
                    : 'text-muted hover:text-fg',
                )}
              >
                {option.label}
                {option.value && (
                  <span className="text-amber ml-1.5 font-mono text-[10.5px]">
                    −2 luni
                  </span>
                )}
              </button>
            ))}
          </div>
        </Reveal>

        <div className="mt-12 grid gap-4 md:grid-cols-3">
          {plans.map((plan, i) => (
            <Reveal key={plan.name} delay={i * 90}>
              <PlanCard plan={plan} annual={annual} />
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  )
}

function PlanCard({ plan, annual }: { plan: Plan; annual: boolean }) {
  // Anual = 2 luni gratuite, deci se plateste 10 din 12
  const price = annual ? Math.round((plan.monthly * 10) / 12) : plan.monthly

  return (
    <div
      className={cn(
        'rounded-card flex h-full flex-col border p-6',
        plan.popular
          ? 'border-amber/45 bg-surface shadow-[0_0_0_1px_rgba(245,166,35,0.08),0_30px_70px_-40px_rgba(245,166,35,0.35)]'
          : 'border-line bg-surface',
      )}
    >
      {/* Inaltime fixa: altfel badge-ul POPULAR decaleaza tot cardul PRO */}
      <div className="flex h-6 items-center justify-between">
        <h3 className="font-mono text-[11px] tracking-[0.14em] uppercase">
          {plan.name}
        </h3>
        {plan.popular && (
          <span className="border-amber/40 text-amber rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-widest uppercase">
            popular
          </span>
        )}
      </div>

      <p className="text-muted mt-3 text-[13.5px] leading-relaxed">
        {plan.blurb}
      </p>

      <p className="mt-6 flex items-baseline gap-1.5">
        <span className="tnum text-[38px] leading-none font-medium tracking-[-0.04em]">
          {price}
        </span>
        <span className="text-muted text-[13.5px]">RON / lună</span>
      </p>
      <p className="text-muted mt-2 font-mono text-[10.5px]">
        {annual ? 'facturat anual · 2 luni gratuite' : 'fără contract, anulezi oricând'}
      </p>

      <ul className="border-line mt-6 flex-1 space-y-3 border-t pt-6">
        {plan.features.map((feature) => (
          <li key={feature} className="flex items-start gap-2.5 text-[13.5px]">
            <Check
              aria-hidden
              className={cn(
                'mt-0.5 size-3.5 shrink-0',
                plan.popular ? 'text-amber' : 'text-muted',
              )}
            />
            {feature}
          </li>
        ))}
      </ul>

      <Link
        to="/signup"
        className={buttonClasses({
          variant: plan.popular ? 'primary' : 'outline',
          className: 'mt-7 w-full',
        })}
      >
        Începe gratuit
      </Link>
    </div>
  )
}

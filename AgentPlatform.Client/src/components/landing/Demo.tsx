import { useEffect, useState } from 'react'
import { useReveal } from '@/hooks/useReveal'
import { BRAND } from '@/lib/brand'
import { cn } from '@/lib/utils'

type Step = {
  time: string
  from: 'lead' | 'ai'
  message: string
  trace: string
}

/**
 * O conversatie reala de noapte, de la intrebare la vizionare programata.
 * Orele sunt structura sectiunii: ele spun ce vinde produsul.
 */
const steps: Step[] = [
  {
    time: '02:14',
    from: 'lead',
    message: 'Bună seara, mai e disponibil apartamentul din Gheorgheni?',
    trace: 'intenție: verificare disponibilitate',
  },
  {
    time: '02:14',
    from: 'ai',
    message: 'Bună, Andrei. Da, e disponibil — 2 camere, 58 m², etaj 3 din 4, la 92.000 €.',
    trace: 'listare identificată · GHE-2C-58',
  },
  {
    time: '02:15',
    from: 'lead',
    message: 'Da, dar am maxim 100.000 și vreau cu parcare',
    trace: 'buget ≤ 100.000 € · cerință: parcare',
  },
  {
    time: '02:15',
    from: 'ai',
    message:
      'Am 3 variante sub 100.000 € cu parcare subterană. Îți programez o vizionare mâine la 17:00?',
    trace: 'căutare vectorială · 3 potriviri din 148 listări',
  },
  {
    time: '02:16',
    from: 'lead',
    message: 'Perfect, mâine la 17 e bine',
    trace: 'lead calificat · scor 87 / 100',
  },
  {
    time: '02:16',
    from: 'ai',
    message: 'Gata, te-am programat. Dimineață primești adresa și fișa completă.',
    trace: 'vizionare în calendar · agent notificat',
  },
]

const prefersReduced = () =>
  typeof window !== 'undefined' &&
  window.matchMedia('(prefers-reduced-motion: reduce)').matches

export function Demo() {
  const { ref, shown } = useReveal<HTMLDivElement>('-20% 0px')
  // Cine a cerut mai putina animatie vede conversatia intreaga de la inceput
  const [visible, setVisible] = useState(() =>
    prefersReduced() ? steps.length : 0,
  )

  useEffect(() => {
    if (!shown || prefersReduced()) return

    const timers = steps.map((_, i) =>
      window.setTimeout(() => setVisible(i + 1), 400 + i * 620),
    )
    return () => timers.forEach(window.clearTimeout)
  }, [shown])

  const running = visible > 0 && visible < steps.length

  return (
    <section id="demo" className="border-line scroll-mt-16 border-t px-5 py-24 sm:px-8 sm:py-32">
      <div className="mx-auto max-w-6xl">
        <header className="max-w-2xl">
          <p className="text-amber font-mono text-[11px] tracking-[0.14em] uppercase">
            marți, 02:14
          </p>
          <h2 className="mt-4 text-[30px] leading-[1.1] font-medium tracking-[-0.035em] sm:text-[40px]">
            Vezi cum funcționează
          </h2>
          <p className="text-muted mt-4 text-[16px] leading-relaxed">
            Un lead scrie în miezul nopții. În stânga e ce vede el. În dreapta e
            ce face {BRAND.name} în aceleași două minute.
          </p>
        </header>

        <div ref={ref} className="mt-14">
          <div className="mb-6 hidden lg:grid lg:grid-cols-[1fr_auto_1fr] lg:gap-x-8">
            <p className="text-muted font-mono text-[10.5px] tracking-widest uppercase">
              conversația pe WhatsApp
            </p>
            <span className="w-14" />
            <p className="text-muted font-mono text-[10.5px] tracking-widest uppercase">
              ce face {BRAND.name} în spate
            </p>
          </div>

          <div className="relative">
            {/* Axul vertical pe care stau orele */}
            <div
              aria-hidden
              className="bg-line absolute inset-y-0 left-1/2 hidden w-px -translate-x-1/2 lg:block"
            />

            <div className="space-y-3">
              {steps.map((step, i) => (
                <Row key={i} step={step} active={i < visible} />
              ))}
            </div>
          </div>

          <p
            className={cn(
              'text-muted mt-8 font-mono text-[11px] transition-opacity duration-500',
              visible >= steps.length ? 'opacity-100' : 'opacity-0',
            )}
          >
            <span className="text-amber">rezultat</span> — vizionare programată,
            zero intervenție din partea ta
            {running && <span className="animate-caret ml-1">▍</span>}
          </p>
        </div>
      </div>
    </section>
  )
}

function Row({ step, active }: { step: Step; active: boolean }) {
  return (
    <div
      className={cn(
        'grid gap-2 transition-[opacity,transform] duration-500 ease-out lg:grid-cols-[1fr_auto_1fr] lg:items-center lg:gap-x-8',
        active ? 'translate-y-0 opacity-100' : 'translate-y-2 opacity-0',
      )}
    >
      <p className="bg-ink text-muted tnum relative z-10 w-14 font-mono text-[11px] lg:order-2 lg:py-1 lg:text-center">
        {step.time}
      </p>

      {/* Coloana de chat sta lipita de ax, dar in interiorul ei se pastreaza
          convenția WhatsApp: leadul la stanga, agentul la dreapta. */}
      <div className="lg:order-1 lg:flex lg:justify-end">
        <div
          className={cn(
            'flex w-full lg:w-[26rem]',
            step.from === 'lead' ? 'justify-start' : 'justify-end',
          )}
        >
          <p
            className={cn(
              'max-w-[88%] px-3.5 py-2.5 text-[13.5px] leading-relaxed',
              step.from === 'lead'
                ? 'bg-line-strong/45 rounded-[12px] rounded-bl-[3px]'
                : 'bg-wa rounded-[12px] rounded-br-[3px]',
            )}
          >
            {step.message}
          </p>
        </div>
      </div>

      <p className="border-line text-muted rounded-btn border border-dashed px-3.5 py-2.5 font-mono text-[11.5px] leading-relaxed lg:order-3">
        {step.trace}
      </p>
    </div>
  )
}

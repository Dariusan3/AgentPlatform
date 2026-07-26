import { Link } from 'react-router-dom'
import { buttonClasses } from '@/components/ui/button-variants'
import { BRAND } from '@/lib/brand'

const stats = [
  { value: '500+', label: 'agenți activi' },
  { value: '24/7', label: 'fără pauză' },
  { value: '3×', label: 'mai multe leaduri' },
]

export function Hero() {
  // isolate: fara el, glow-ul cu -z-10 ar cadea sub fundalul paginii
  return (
    <section className="relative isolate overflow-hidden px-5 pt-28 pb-20 sm:px-8 sm:pt-32">
      {/* Lumina de veghe: singurul moment de culoare din fundal */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 -z-10 h-[620px]"
      >
        <div className="animate-breathe absolute top-[-260px] left-1/2 size-[820px] -translate-x-1/2 rounded-full bg-[radial-gradient(circle,rgba(245,166,35,0.20),transparent_60%)]" />
      </div>

      <div className="mx-auto max-w-3xl text-center">
        <p className="animate-fade-up border-line bg-surface/60 text-muted inline-flex items-center gap-2.5 rounded-full border px-3.5 py-1.5 text-[12.5px]">
          <span className="relative flex size-1.5">
            <span className="bg-amber absolute inline-flex size-full animate-ping rounded-full opacity-60" />
            <span className="bg-amber relative inline-flex size-1.5 rounded-full" />
          </span>
          {BRAND.tagline}
        </p>

        <h1
          className="animate-fade-up mt-8 text-[40px] leading-[1.04] font-medium tracking-[-0.045em] text-balance sm:text-[56px] lg:text-[64px]"
          style={{ animationDelay: '80ms' }}
        >
          Agentul tău AI care lucrează{' '}
          <span className="text-amber tnum">24/7</span>
        </h1>

        <p
          className="animate-fade-up text-muted mx-auto mt-6 max-w-xl text-[16.5px] leading-relaxed text-pretty sm:text-[18px]"
          style={{ animationDelay: '160ms' }}
        >
          Răspunde la leaduri pe WhatsApp, califică clienții și închide mai
          multe tranzacții — automat.
        </p>

        <div
          className="animate-fade-up mt-9 flex flex-col items-center justify-center gap-3 sm:flex-row"
          style={{ animationDelay: '240ms' }}
        >
          <Link
            to="/signup"
            className={buttonClasses({
              size: 'lg',
              className: 'w-full sm:w-auto',
            })}
          >
            Începe {BRAND.trialDays} zile gratuit
          </Link>
          <a
            href="#demo"
            className={buttonClasses({
              variant: 'outline',
              size: 'lg',
              className: 'w-full sm:w-auto',
            })}
          >
            Vezi demo
          </a>
        </div>

        <dl
          className="animate-fade-up border-line mx-auto mt-14 flex max-w-lg divide-x divide-line border-y"
          style={{ animationDelay: '320ms' }}
        >
          {stats.map((stat) => (
            <div key={stat.label} className="flex-1 px-2 py-5">
              <dt className="tnum text-[22px] font-medium tracking-[-0.02em] sm:text-[26px]">
                {stat.value}
              </dt>
              <dd className="text-muted mt-1 font-mono text-[10.5px] tracking-[0.06em] uppercase">
                {stat.label}
              </dd>
            </div>
          ))}
        </dl>
      </div>

      <HeroChat />
    </section>
  )
}

/** Un singur schimb de mesaje, la 02:14. Ora e argumentul, nu decorul. */
function HeroChat() {
  return (
    <div
      className="animate-fade-up border-line bg-surface rounded-card mx-auto mt-16 max-w-md border p-4 shadow-[0_24px_60px_-24px_rgba(0,0,0,0.9)] sm:p-5"
      style={{ animationDelay: '420ms' }}
    >
      <div className="border-line flex items-center gap-3 border-b pb-3.5">
        <span className="bg-line-strong grid size-8 place-items-center rounded-full text-[11px] font-medium">
          AM
        </span>
        <span className="flex-1 text-left">
          <span className="block text-[13.5px] font-medium">Andrei M.</span>
          <span className="text-muted block font-mono text-[10.5px]">
            lead nou · Cluj
          </span>
        </span>
        <span className="text-muted tnum font-mono text-[11px]">02:14</span>
      </div>

      <div className="space-y-2.5 pt-4 text-left text-[13.5px] leading-relaxed">
        <p className="bg-line-strong/45 max-w-[85%] rounded-[12px] rounded-bl-[3px] px-3.5 py-2.5">
          Bună seara, mai e disponibil apartamentul din Gheorgheni?
        </p>
        <p className="bg-wa ml-auto max-w-[88%] rounded-[12px] rounded-br-[3px] px-3.5 py-2.5">
          Bună, Andrei. Da, e disponibil — 2 camere, 58 m², etaj 3 din 4, la
          92.000 €. Vrei să-ți trimit și alte variante în zonă?
        </p>
      </div>

      <p className="border-line text-muted mt-4 flex items-center justify-between border-t pt-3.5 font-mono text-[10.5px]">
        <span>tu dormeai</span>
        <span className="text-amber tnum">răspuns în 4 s</span>
      </p>
    </div>
  )
}

import { Star } from 'lucide-react'
import { Reveal } from '@/components/Reveal'

const testimonials = [
  {
    quote:
      'Nu mai stau cu telefonul în mână la 11 seara. Dimineața am conversațiile gata filtrate și știu pe cine să sun primul.',
    metric: '3 ore pe zi',
    name: 'Maria Ionescu',
    role: 'agent independent',
    city: 'Cluj-Napoca',
  },
  {
    quote:
      'Am pus 40 de listări în sistem și l-am lăsat să răspundă singur o lună. A prins patru tranzacții pe care altfel le pierdeam.',
    metric: '4 tranzacții extra',
    name: 'Alexandru Pop',
    role: 'agenție cu 6 agenți',
    city: 'București',
  },
  {
    quote:
      'Clienții mei sunt surprinși că primesc răspuns la 2 dimineața. Cred că lucrez non-stop. Nu îi contrazic.',
    metric: 'răspuns în secunde',
    name: 'Elena Mureșan',
    role: 'agent rezidențial',
    city: 'Timișoara',
  },
]

export function Testimonials() {
  return (
    <section
      id="testimonials"
      className="border-line scroll-mt-16 border-t px-5 py-24 sm:px-8 sm:py-32"
    >
      <div className="mx-auto max-w-6xl">
        <Reveal>
          <header className="max-w-xl">
            <p className="text-amber font-mono text-[11px] tracking-[0.14em] uppercase">
              testimoniale
            </p>
            <h2 className="mt-4 text-[30px] leading-[1.1] font-medium tracking-[-0.035em] sm:text-[40px]">
              Ce spun agenții noștri
            </h2>
          </header>
        </Reveal>

        <div className="mt-12 grid gap-4 md:grid-cols-3">
          {testimonials.map((item, i) => (
            <Reveal key={item.name} delay={i * 90}>
              <figure className="border-line bg-surface rounded-card flex h-full flex-col p-6">
                <div className="flex gap-0.5" aria-label="5 din 5 stele">
                  {Array.from({ length: 5 }, (_, s) => (
                    <Star
                      key={s}
                      aria-hidden
                      className="text-amber size-3.5 fill-current"
                    />
                  ))}
                </div>

                <p className="text-amber mt-5 font-mono text-[11px] tracking-[0.08em] uppercase">
                  {item.metric}
                </p>

                <blockquote className="mt-3 flex-1 text-[14.5px] leading-relaxed">
                  {item.quote}
                </blockquote>

                <figcaption className="border-line mt-6 flex items-center gap-3 border-t pt-5">
                  <span className="border-line text-amber grid size-9 shrink-0 place-items-center rounded-full border font-mono text-[11px]">
                    {item.name
                      .split(' ')
                      .map((word) => word[0])
                      .join('')}
                  </span>
                  <span>
                    <span className="block text-[13.5px] font-medium">
                      {item.name}
                    </span>
                    <span className="text-muted block font-mono text-[10.5px]">
                      {item.role} · {item.city}
                    </span>
                  </span>
                </figcaption>
              </figure>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  )
}

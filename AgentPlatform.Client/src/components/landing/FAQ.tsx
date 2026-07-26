import { Reveal } from '@/components/Reveal'
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '@/components/ui/accordion'
import { BRAND } from '@/lib/brand'

const faqs = [
  {
    q: 'Cum funcționează perioada de trial?',
    a: `Ai ${BRAND.trialDays} zile complete, fără card bancar. Îți conectezi numărul de WhatsApp, încarci listările și agentul începe să răspundă. Dacă la final nu continui, conversațiile rămân ale tale și le poți exporta.`,
  },
  {
    q: 'Am nevoie de cont WhatsApp Business?',
    a: 'Da, un cont WhatsApp Business gratuit e suficient. Îl conectezi în aproximativ cinci minute din panou, cu un cod de verificare. Numărul tău personal rămâne separat.',
  },
  {
    q: 'Poate AI-ul să răspundă în română?',
    a: `Româna e limba principală, cu diacritice și în registru natural. ${BRAND.name} înțelege și cum scriu clienții în realitate — fără diacritice, cu prescurtări, cu greșeli de tastare. Răspunde și în engleză când clientul scrie în engleză.`,
  },
  {
    q: 'Datele mele sunt în siguranță?',
    a: 'Fiecare agent are baza lui de date, izolată la nivel de rând, iar datele stau pe servere din Uniunea Europeană. Nu antrenăm modele pe conversațiile tale și nu le împărtășim cu alți clienți.',
  },
  {
    q: 'Pot anula oricând?',
    a: 'Da. Abonamentul lunar se oprește la finalul perioadei plătite, dintr-un singur buton, fără să vorbești cu nimeni. Pentru planul anual returnăm proporțional lunile nefolosite.',
  },
  {
    q: 'Ce se întâmplă dacă depășesc limita de conversații?',
    a: 'Agentul nu se oprește brusc. Te anunțăm la 80% din limită și continui la 0,15 RON pe conversație suplimentară, sau treci pe Pro, unde conversațiile sunt nelimitate.',
  },
]

export function FAQ() {
  return (
    <section
      id="faq"
      className="border-line scroll-mt-16 border-t px-5 py-24 sm:px-8 sm:py-32"
    >
      <div className="mx-auto grid max-w-6xl gap-12 lg:grid-cols-[0.8fr_1.2fr] lg:gap-20">
        <Reveal>
          <header className="lg:sticky lg:top-28">
            <p className="text-amber font-mono text-[11px] tracking-[0.14em] uppercase">
              întrebări
            </p>
            <h2 className="mt-4 text-[30px] leading-[1.1] font-medium tracking-[-0.035em] sm:text-[40px]">
              Întrebări frecvente
            </h2>
            <p className="text-muted mt-4 text-[15px] leading-relaxed">
              Nu găsești ce cauți? Scrie-ne la{' '}
              <a
                href="mailto:salut@portar.ro"
                className="text-fg underline decoration-line-strong underline-offset-4 transition-colors hover:decoration-amber"
              >
                salut@portar.ro
              </a>
              .
            </p>
          </header>
        </Reveal>

        <Reveal delay={90}>
          <Accordion type="single" collapsible className="border-line border-t">
            {faqs.map((faq) => (
              <AccordionItem key={faq.q} value={faq.q}>
                <AccordionTrigger>{faq.q}</AccordionTrigger>
                <AccordionContent>{faq.a}</AccordionContent>
              </AccordionItem>
            ))}
          </Accordion>
        </Reveal>
      </div>
    </section>
  )
}

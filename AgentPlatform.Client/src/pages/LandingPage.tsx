import { useEffect } from 'react'
import { Link } from 'react-router-dom'
import { Navbar } from '@/components/landing/Navbar'
import { Hero } from '@/components/landing/Hero'
import { Demo } from '@/components/landing/Demo'
import { Pricing } from '@/components/landing/Pricing'
import { Testimonials } from '@/components/landing/Testimonials'
import { FAQ } from '@/components/landing/FAQ'
import { Footer } from '@/components/landing/Footer'
import { Reveal } from '@/components/Reveal'
import { buttonClasses } from '@/components/ui/button-variants'
import { BRAND } from '@/lib/brand'

export function LandingPage() {
  // Browserul caută ancora din URL inainte ca React sa randeze secțiunile,
  // deci un link direct spre /#pricing nu ar sari. Reluam scroll-ul dupa montare.
  useEffect(() => {
    const id = window.location.hash.slice(1)
    if (!id) return
    document.getElementById(id)?.scrollIntoView({ block: 'start' })
  }, [])

  return (
    <>
      <Navbar />
      <main>
        <Hero />
        <Demo />
        <Pricing />
        <Testimonials />
        <FAQ />
        <FinalCta />
      </main>
      <Footer />
    </>
  )
}

function FinalCta() {
  return (
    <section className="border-line bg-surface border-t px-5 py-24 sm:px-8 sm:py-32">
      <Reveal className="mx-auto max-w-2xl text-center">
        <h2 className="text-[30px] leading-[1.1] font-medium tracking-[-0.035em] text-balance sm:text-[40px]">
          Gata să închizi mai multe tranzacții?
        </h2>
        <p className="text-muted mt-4 text-[16px]">
          {BRAND.trialDays} zile gratuit, fără card bancar.
        </p>
        <Link
          to="/signup"
          className={buttonClasses({ size: 'lg', className: 'mt-9' })}
        >
          Începe acum gratuit
        </Link>
      </Reveal>
    </section>
  )
}

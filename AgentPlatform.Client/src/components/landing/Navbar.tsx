import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Logo } from '@/components/Logo'
import { buttonClasses } from '@/components/ui/button-variants'
import { BRAND } from '@/lib/brand'
import { cn } from '@/lib/utils'

const links = [
  { href: '#demo', label: 'Cum funcționează' },
  { href: '#pricing', label: 'Prețuri' },
  { href: '#testimonials', label: 'Testimoniale' },
  { href: '#faq', label: 'Întrebări' },
]

export function Navbar() {
  const [scrolled, setScrolled] = useState(false)

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 12)
    onScroll()
    window.addEventListener('scroll', onScroll, { passive: true })
    return () => window.removeEventListener('scroll', onScroll)
  }, [])

  return (
    <header
      className={cn(
        'fixed inset-x-0 top-0 z-50 transition-colors duration-300',
        scrolled
          ? 'border-line bg-ink/70 border-b backdrop-blur-xl'
          : 'border-b border-transparent',
      )}
    >
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-5 sm:px-8">
        <Link to="/" aria-label={`${BRAND.name} — acasă`}>
          <Logo />
        </Link>

        <nav className="hidden items-center gap-8 md:flex">
          {links.map((link) => (
            <a
              key={link.href}
              href={link.href}
              className="text-muted hover:text-fg text-[13.5px] transition-colors"
            >
              {link.label}
            </a>
          ))}
        </nav>

        <Link
          to="/signup"
          className={buttonClasses({
            variant: 'outline',
            size: 'sm',
            className:
              'border-amber/35 text-amber hover:border-amber hover:bg-amber/[0.07]',
          })}
        >
          Începe gratuit
        </Link>
      </div>
    </header>
  )
}

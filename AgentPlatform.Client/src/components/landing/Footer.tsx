import { Logo } from '@/components/Logo'
import { BRAND } from '@/lib/brand'

const links = [
  { href: '#', label: 'Termeni' },
  { href: '#', label: 'GDPR' },
  { href: 'mailto:salut@portar.ro', label: 'Contact' },
]

export function Footer() {
  return (
    <footer className="border-line border-t px-5 py-14 sm:px-8">
      <div className="mx-auto flex max-w-6xl flex-col gap-10 md:flex-row md:items-start md:justify-between">
        <div className="max-w-xs">
          <Logo />
          <p className="text-muted mt-4 text-[13.5px] leading-relaxed">
            Agentul care stă la intrare, întreabă ce trebuie și nu doarme
            niciodată.
          </p>
        </div>

        <nav className="flex gap-8">
          {links.map((link) => (
            <a
              key={link.label}
              href={link.href}
              className="text-muted hover:text-fg text-[13.5px] transition-colors"
            >
              {link.label}
            </a>
          ))}
        </nav>
      </div>

      {/* Sans, nu mono, si mai luminos: mono la 11px pe negru se citea prost */}
      <p className="border-line text-fg/60 mx-auto mt-12 max-w-6xl border-t pt-8 text-[13px]">
        © 2026 {BRAND.name}. Construit pentru agenții imobiliari români.
      </p>
    </footer>
  )
}

import { useAuth } from '@/hooks/useAuth'
import { BRAND } from '@/lib/brand'

export function Dashboard() {
  const { user } = useAuth()

  // full_name ajunge in metadata la signUp, deci nu e nevoie de query
  const fullName =
    typeof user?.user_metadata?.full_name === 'string'
      ? user.user_metadata.full_name
      : null
  const firstName = fullName?.split(' ')[0]

  return (
    <>
      <p className="text-amber font-mono text-[11px] tracking-[0.14em] uppercase">
        panou
      </p>
      <h1 className="mt-4 text-[30px] leading-[1.1] font-medium tracking-[-0.035em] sm:text-[38px]">
        Bine ai venit{firstName ? `, ${firstName}` : ''}!
      </h1>
      <p className="text-muted mt-4 max-w-xl text-[16px] leading-relaxed">
        Contul e activ. Următorul pas e să conectezi numărul de WhatsApp și să
        încarci primele listări, ca {BRAND.name} să aibă cu ce răspunde.
      </p>

      <dl className="border-line rounded-card bg-surface mt-10 grid gap-px overflow-hidden border sm:grid-cols-3">
        <Cell label="email" value={user?.email ?? '—'} />
        <Cell label="tenant id" value={user?.id ?? '—'} mono />
        <Cell
          label="cont creat"
          value={
            user?.created_at
              ? new Date(user.created_at).toLocaleDateString('ro-RO', {
                  day: 'numeric',
                  month: 'long',
                  year: 'numeric',
                })
              : '—'
          }
        />
      </dl>
    </>
  )
}

function Cell({
  label,
  value,
  mono,
}: {
  label: string
  value: string
  mono?: boolean
}) {
  return (
    <div className="border-line p-5 sm:border-r sm:last:border-r-0">
      <dt className="text-muted font-mono text-[10.5px] tracking-widest uppercase">
        {label}
      </dt>
      <dd
        className={
          mono
            ? 'mt-2 truncate font-mono text-[12px]'
            : 'mt-2 truncate text-[14px]'
        }
        title={value}
      >
        {value}
      </dd>
    </div>
  )
}

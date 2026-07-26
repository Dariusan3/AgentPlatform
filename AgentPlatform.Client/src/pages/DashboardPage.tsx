import {
  ArrowUpRight,
  Building2,
  MessageSquare,
  TrendingUp,
  Users,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { Link } from 'react-router-dom'
import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { ErrorCard, LoadingCard } from '@/components/QueryState'
import { Avatar } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import { Card, CardBody, CardHeader, CardTitle } from '@/components/ui/card'
import {
  conversationStatusLabels,
  formatMessageTime,
  formatWeekday,
} from '@/lib/labels'
import { useDashboardStats } from '@/lib/queries/useDashboard'
import { conversationTone } from '@/lib/tones'
import type { DashboardStats } from '@/lib/types'
import { cn } from '@/lib/utils'

export function DashboardPage() {
  const { data, isPending, isError, error, refetch } = useDashboardStats()

  if (isPending) {
    return (
      <div className="mx-auto max-w-6xl">
        <LoadingCard label="Se încarcă statisticile…" />
      </div>
    )
  }

  if (isError) {
    return (
      <div className="mx-auto max-w-6xl">
        <ErrorCard error={error} onRetry={() => void refetch()} />
      </div>
    )
  }

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <StatCards stats={data} />
      <ActivityChart stats={data} />
      <RecentConversations stats={data} />
    </div>
  )
}

function StatCards({ stats }: { stats: DashboardStats }) {
  const cards: {
    label: string
    value: string
    note: string
    icon: LucideIcon
    iconClass: string
  }[] = [
    {
      label: 'Conversații active',
      value: String(stats.activeConversations),
      note: 'în derulare acum',
      icon: MessageSquare,
      iconClass: 'text-amber',
    },
    {
      label: 'Leaduri noi',
      value: String(stats.newLeads),
      note: 'în ultimele 7 zile',
      icon: Users,
      iconClass: 'text-info',
    },
    {
      label: 'Proprietăți',
      value: String(stats.totalProperties),
      note: 'în portofoliu',
      icon: Building2,
      iconClass: 'text-violet',
    },
    {
      label: 'Rată de conversie',
      value: `${stats.conversionRate}%`,
      note: 'din conversații în leaduri',
      icon: TrendingUp,
      iconClass: 'text-success',
    },
  ]

  return (
    <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
      {cards.map((card) => (
        <Card key={card.label} className="p-5">
          <div className="flex items-start justify-between gap-3">
            <p className="text-muted font-mono text-[10.5px] tracking-widest uppercase">
              {card.label}
            </p>
            <card.icon
              aria-hidden
              className={cn('size-4 shrink-0', card.iconClass)}
            />
          </div>

          <p className="tnum mt-4 text-[30px] leading-none font-medium tracking-[-0.04em]">
            {card.value}
          </p>

          <p className="text-muted mt-2.5 text-[12px] leading-relaxed">
            {card.note}
          </p>
        </Card>
      ))}
    </div>
  )
}

function ActivityChart({ stats }: { stats: DashboardStats }) {
  // Recharts are nevoie de eticheta pe axa, nu de data ISO
  const data = stats.weeklyData.map((point) => ({
    day: formatWeekday(point.date),
    conversatii: point.conversations,
    leaduri: point.leads,
  }))

  const hasActivity = stats.weeklyData.some(
    (point) => point.conversations > 0 || point.leads > 0,
  )

  return (
    <Card>
      <CardHeader>
        <div>
          <CardTitle>Activitate săptămânală</CardTitle>
          <p className="text-muted mt-1 text-[12.5px]">
            Conversații purtate și leaduri calificate, pe zile
          </p>
        </div>
        <div className="hidden items-center gap-4 sm:flex">
          <Legend className="bg-amber" label="Conversații" />
          <Legend className="bg-fg" label="Leaduri" />
        </div>
      </CardHeader>
      <CardBody>
        <div className="relative h-64 w-full">
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart
              data={data}
              margin={{ top: 4, right: 4, bottom: 0, left: -22 }}
            >
              <defs>
                <linearGradient id="fillConversatii" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#f5a623" stopOpacity={0.16} />
                  <stop offset="100%" stopColor="#f5a623" stopOpacity={0} />
                </linearGradient>
                <linearGradient id="fillLeaduri" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#ffffff" stopOpacity={0.08} />
                  <stop offset="100%" stopColor="#ffffff" stopOpacity={0} />
                </linearGradient>
              </defs>

              <CartesianGrid stroke="#1a1a1a" vertical={false} />
              <XAxis
                dataKey="day"
                stroke="#888888"
                tickLine={false}
                axisLine={false}
                tick={{ fontSize: 11, fontFamily: 'JetBrains Mono' }}
                dy={8}
              />
              <YAxis
                stroke="#888888"
                tickLine={false}
                axisLine={false}
                tick={{ fontSize: 11, fontFamily: 'JetBrains Mono' }}
                width={48}
                allowDecimals={false}
              />
              <Tooltip
                content={<ChartTooltip />}
                cursor={{ stroke: '#333333', strokeDasharray: '3 3' }}
              />

              <Area
                type="monotone"
                dataKey="conversatii"
                name="Conversații"
                stroke="#f5a623"
                strokeWidth={2}
                fill="url(#fillConversatii)"
                dot={false}
                activeDot={{
                  r: 3.5,
                  fill: '#f5a623',
                  stroke: '#0a0a0a',
                  strokeWidth: 2,
                }}
              />
              <Area
                type="monotone"
                dataKey="leaduri"
                name="Leaduri"
                stroke="#ffffff"
                strokeWidth={2}
                fill="url(#fillLeaduri)"
                dot={false}
                activeDot={{
                  r: 3.5,
                  fill: '#ffffff',
                  stroke: '#0a0a0a',
                  strokeWidth: 2,
                }}
              />
            </AreaChart>
          </ResponsiveContainer>

          {/* Un grafic plat nu spune nimic singur; explicam de ce e gol */}
          {!hasActivity && (
            <p className="text-muted absolute inset-0 grid place-items-center text-center text-[13px]">
              Încă nicio activitate săptămâna asta.
            </p>
          )}
        </div>
      </CardBody>
    </Card>
  )
}

function RecentConversations({ stats }: { stats: DashboardStats }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Activitate recentă</CardTitle>
        <Link
          to="/dashboard/conversations"
          className="text-amber hover:text-amber/80 flex shrink-0 items-center gap-1 text-[13px] transition-colors"
        >
          Vezi toate
          <ArrowUpRight aria-hidden className="size-3.5" />
        </Link>
      </CardHeader>

      {stats.recentConversations.length === 0 ? (
        <CardBody>
          <p className="text-muted text-[13.5px] leading-relaxed">
            Nicio conversație încă. Când un client scrie pe WhatsApp, apare aici
            automat.
          </p>
        </CardBody>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[38rem] text-left">
            <thead>
              <tr className="border-line border-b">
                {[
                  'Contact',
                  'Canal',
                  'Status',
                  'Lead score',
                  'Ultimul mesaj',
                ].map((head) => (
                  <th
                    key={head}
                    className="text-muted px-5 py-3 font-mono text-[10.5px] font-normal tracking-widest uppercase"
                  >
                    {head}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {stats.recentConversations.map((conversation) => (
                <tr
                  key={conversation.id}
                  className="border-line hover:bg-hover border-b transition-colors last:border-b-0"
                >
                  <td className="px-5 py-3.5">
                    <div className="flex items-center gap-2.5">
                      <Avatar
                        name={
                          conversation.contactName ?? conversation.contactPhone
                        }
                        className="size-7"
                      />
                      <span className="text-[13.5px]">
                        {conversation.contactName ?? conversation.contactPhone}
                      </span>
                    </div>
                  </td>
                  <td className="text-muted px-5 py-3.5 text-[13px] capitalize">
                    {conversation.channel}
                  </td>
                  <td className="px-5 py-3.5">
                    <Badge tone={conversationTone[conversation.status]}>
                      {conversationStatusLabels[conversation.status]}
                    </Badge>
                  </td>
                  <td className="tnum px-5 py-3.5 font-mono text-[12.5px]">
                    {conversation.leadScore}
                  </td>
                  <td className="text-muted tnum px-5 py-3.5 font-mono text-[12px]">
                    {conversation.lastMessageAt
                      ? formatMessageTime(conversation.lastMessageAt)
                      : '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Card>
  )
}

function Legend({ className, label }: { className: string; label: string }) {
  return (
    <span className="text-muted flex items-center gap-2 font-mono text-[10.5px] tracking-widest uppercase">
      <span className={cn('h-0.5 w-4 rounded-full', className)} />
      {label}
    </span>
  )
}

type TooltipPayload = {
  active?: boolean
  label?: string
  payload?: { name?: string; value?: number; color?: string }[]
}

function ChartTooltip({ active, label, payload }: TooltipPayload) {
  if (!active || !payload?.length) return null

  return (
    <div className="border-line bg-raised rounded-btn min-w-36 border px-3 py-2.5 shadow-[0_16px_40px_-16px_rgba(0,0,0,0.9)]">
      <p className="text-muted font-mono text-[10.5px] tracking-widest uppercase">
        {label}
      </p>
      <div className="mt-2 space-y-1.5">
        {payload.map((entry) => (
          <p
            key={entry.name}
            className="flex items-center justify-between gap-4 text-[12.5px]"
          >
            <span className="flex items-center gap-2">
              <span
                className="size-1.5 rounded-full"
                style={{ backgroundColor: entry.color }}
              />
              {entry.name}
            </span>
            <span className="tnum font-mono">{entry.value}</span>
          </p>
        ))}
      </div>
    </div>
  )
}

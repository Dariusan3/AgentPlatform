import { useState } from 'react'
import { CheckCircle2, MoreHorizontal, Trash2, Users } from 'lucide-react'
import { PageHeader } from '@/components/layout/PageHeader'
import { EmptyCard, ErrorCard, LoadingCard } from '@/components/QueryState'
import { Avatar } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Progress } from '@/components/ui/progress'
import {
  Sheet,
  SheetBody,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Spinner } from '@/components/Spinner'
import {
  formatEur,
  formatMessageTime,
  formatShortDate,
  leadStatusLabels,
  leadStatuses,
} from '@/lib/labels'
import {
  useDeleteLead,
  useLead,
  useLeads,
  useUpdateLeadStatus,
} from '@/lib/queries/useLeads'
import { leadTone, scoreClass } from '@/lib/tones'
import type { Lead, LeadStatus } from '@/lib/types'
import { cn } from '@/lib/utils'

type Filter = 'all' | LeadStatus

const filters: { value: Filter; label: string }[] = [
  { value: 'all', label: 'Toate' },
  { value: 'new', label: 'Noi' },
  { value: 'contacted', label: 'Contactați' },
  { value: 'qualified', label: 'Calificați' },
  { value: 'lost', label: 'Pierduți' },
]

export function LeadsPage() {
  const [filter, setFilter] = useState<Filter>('all')
  const [openLeadId, setOpenLeadId] = useState<string | null>(null)

  // Numaratoarea pe taburi are nevoie de lista completa, indiferent de filtru
  const all = useLeads()
  const filtered = useLeads(filter === 'all' ? undefined : filter)
  const updateStatus = useUpdateLeadStatus()
  const deleteLead = useDeleteLead()

  const counts = (all.data ?? []).reduce<Record<string, number>>(
    (acc, lead) => {
      acc.all = (acc.all ?? 0) + 1
      acc[lead.status] = (acc[lead.status] ?? 0) + 1
      return acc
    },
    {},
  )

  const leads = filtered.data ?? []

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <PageHeader
        title="Leaduri"
        count={all.data?.length}
        description="Contacte pe care agenții AI le-au calificat din conversații. Scorul reflectă cât de aproape e clientul de o vizionare."
      />

      <Tabs value={filter} onValueChange={(value) => setFilter(value as Filter)}>
        <TabsList>
          {filters.map((item) => (
            <TabsTrigger key={item.value} value={item.value}>
              {item.label}
              <span className="text-muted tnum font-mono text-[11px]">
                {counts[item.value] ?? 0}
              </span>
            </TabsTrigger>
          ))}
        </TabsList>
      </Tabs>

      {filtered.isPending ? (
        <LoadingCard label="Se încarcă leadurile…" />
      ) : filtered.isError ? (
        <ErrorCard error={filtered.error} onRetry={() => void filtered.refetch()} />
      ) : leads.length === 0 ? (
        <EmptyCard
          icon={<Users aria-hidden className="size-6" />}
          title={filter === 'all' ? 'Încă niciun lead' : 'Niciun lead aici'}
          description={
            filter === 'all'
              ? 'Când agenții califică un contact dintr-o conversație, apare automat în lista asta.'
              : `Nicio intrare cu statusul „${leadStatusLabels[filter as LeadStatus]}".`
          }
        />
      ) : (
        <Card className="overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[52rem] text-left">
              <thead>
                <tr className="border-line border-b">
                  {['Contact', 'Buget', 'Oraș', 'Tip', 'Status', 'Scor', 'Data', ''].map(
                    (head, index) => (
                      <th
                        key={head || index}
                        className="text-muted px-4 py-3 first:pl-5 font-mono text-[10.5px] font-normal tracking-widest uppercase"
                      >
                        {head}
                      </th>
                    ),
                  )}
                </tr>
              </thead>
              <tbody>
                {leads.map((lead) => (
                  <tr
                    key={lead.id}
                    onClick={() => setOpenLeadId(lead.id)}
                    className="border-line hover:bg-hover cursor-pointer border-b transition-colors last:border-b-0"
                  >
                    <td className="px-4 py-3.5 pl-5">
                      <div className="flex items-center gap-2.5">
                        <Avatar
                          name={lead.name ?? lead.phone ?? '?'}
                          className="size-7"
                        />
                        <span className="min-w-0">
                          <span className="block truncate text-[13.5px]">
                            {lead.name ?? 'Fără nume'}
                          </span>
                          <span className="text-muted tnum block font-mono text-[10.5px]">
                            {lead.phone ?? '—'}
                          </span>
                        </span>
                      </div>
                    </td>
                    <td className="tnum px-4 py-3.5 font-mono text-[12px] whitespace-nowrap">
                      {lead.budgetMin !== null && lead.budgetMax !== null
                        ? `${formatEur(lead.budgetMin)} – ${formatEur(lead.budgetMax)} €`
                        : '—'}
                    </td>
                    <td className="px-4 py-3.5 text-[13px]">
                      {lead.preferredCity ?? '—'}
                    </td>
                    <td className="text-muted px-4 py-3.5 text-[13px]">
                      {lead.preferredType ?? '—'}
                    </td>
                    <td className="px-4 py-3.5">
                      <Badge tone={leadTone[lead.status]}>
                        {leadStatusLabels[lead.status]}
                      </Badge>
                    </td>
                    <td className="px-4 py-3.5">
                      <ScoreBar score={lead.conversation?.leadScore ?? 0} />
                    </td>
                    <td className="text-muted tnum px-4 py-3.5 font-mono text-[11.5px] whitespace-nowrap">
                      {formatShortDate(lead.createdAt)}
                    </td>
                    <td
                      className="px-4 py-3.5 text-right"
                      onClick={(event) => event.stopPropagation()}
                    >
                      <DropdownMenu>
                        <DropdownMenuTrigger
                          aria-label={`Acțiuni pentru ${lead.name ?? 'lead'}`}
                          className="text-muted hover:text-fg hover:bg-raised rounded-btn grid size-8 place-items-center transition-colors"
                        >
                          <MoreHorizontal aria-hidden className="size-4" />
                        </DropdownMenuTrigger>
                        <DropdownMenuContent>
                          {leadStatuses
                            .filter((status) => status !== lead.status)
                            .map((status) => (
                              <DropdownMenuItem
                                key={status}
                                onSelect={() =>
                                  updateStatus.mutate({ id: lead.id, status })
                                }
                              >
                                <CheckCircle2 aria-hidden />
                                Marchează {leadStatusLabels[status].toLowerCase()}
                              </DropdownMenuItem>
                            ))}
                          <DropdownMenuItem
                            tone="danger"
                            onSelect={() => deleteLead.mutate(lead)}
                          >
                            <Trash2 aria-hidden />
                            Șterge
                          </DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      )}

      <LeadSheet leadId={openLeadId} onClose={() => setOpenLeadId(null)} />
    </div>
  )
}

function ScoreBar({ score }: { score: number }) {
  return (
    <div className="flex w-24 items-center gap-2">
      <Progress
        value={score}
        indicatorClassName={scoreClass(score)}
        className="flex-1"
      />
      <span className="tnum font-mono text-[11px]">{score}</span>
    </div>
  )
}

/** Detaliul se cere separat: doar el aduce conversatia si mesajele. */
function LeadSheet({
  leadId,
  onClose,
}: {
  leadId: string | null
  onClose: () => void
}) {
  const { data: lead, isPending, isError, error } = useLead(leadId)
  const updateStatus = useUpdateLeadStatus()

  return (
    <Sheet open={leadId !== null} onOpenChange={(open) => !open && onClose()}>
      <SheetContent>
        {isPending ? (
          <div className="grid flex-1 place-items-center">
            <Spinner className="size-5" />
          </div>
        ) : isError ? (
          <div className="p-6">
            <ErrorCard error={error} />
          </div>
        ) : (
          <LeadDetail
            lead={lead}
            onQualify={() => {
              updateStatus.mutate({ id: lead.id, status: 'qualified' })
              onClose()
            }}
          />
        )}
      </SheetContent>
    </Sheet>
  )
}

function LeadDetail({
  lead,
  onQualify,
}: {
  lead: Lead
  onQualify: () => void
}) {
  const messages = lead.conversation?.messages ?? []

  return (
    <>
      <SheetHeader>
        <div className="flex items-center gap-3">
          <Avatar
            name={lead.name ?? lead.phone ?? '?'}
            tinted
            className="size-10 text-[12px]"
          />
          <div className="min-w-0">
            <SheetTitle>{lead.name ?? 'Fără nume'}</SheetTitle>
            <SheetDescription className="tnum font-mono text-[11px]">
              {lead.phone ?? '—'}
            </SheetDescription>
          </div>
        </div>
      </SheetHeader>

      <SheetBody className="space-y-6">
        <div className="flex items-center gap-3">
          <Badge tone={leadTone[lead.status]}>
            {leadStatusLabels[lead.status]}
          </Badge>
          <div className="flex flex-1 items-center gap-2">
            <Progress
              value={lead.conversation?.leadScore ?? 0}
              indicatorClassName={scoreClass(lead.conversation?.leadScore ?? 0)}
            />
            <span className="tnum font-mono text-[11px]">
              {lead.conversation?.leadScore ?? 0}/100
            </span>
          </div>
        </div>

        <dl className="border-line rounded-card grid grid-cols-2 gap-px overflow-hidden border">
          <Detail
            label="buget"
            value={
              lead.budgetMin !== null && lead.budgetMax !== null
                ? `${formatEur(lead.budgetMin)} – ${formatEur(lead.budgetMax)} €`
                : '—'
            }
          />
          <Detail label="oraș" value={lead.preferredCity ?? '—'} />
          <Detail label="tip" value={lead.preferredType ?? '—'} />
          <Detail label="primul contact" value={formatShortDate(lead.createdAt)} />
        </dl>

        {lead.email && (
          <div>
            <h3 className="text-muted font-mono text-[10.5px] tracking-widest uppercase">
              Email
            </h3>
            <p className="mt-2 text-[13.5px]">{lead.email}</p>
          </div>
        )}

        <div>
          <h3 className="text-muted font-mono text-[10.5px] tracking-widest uppercase">
            Notițe
          </h3>
          <p className="mt-3 text-[13.5px] leading-relaxed">
            {lead.notes ?? 'Fără notițe încă.'}
          </p>
        </div>

        {messages.length > 0 && (
          <div>
            <h3 className="text-muted font-mono text-[10.5px] tracking-widest uppercase">
              Istoricul conversației
            </h3>
            <div className="mt-3 space-y-2">
              {messages.map((message) => (
                <div
                  key={message.id}
                  className={cn(
                    'max-w-[85%] px-3 py-2 text-[13px] leading-relaxed',
                    message.role === 'user'
                      ? 'bg-raised rounded-[12px] rounded-bl-[3px]'
                      : 'ml-auto rounded-[12px] rounded-br-[3px] bg-[#1f1a0f]',
                  )}
                >
                  {message.content}
                  <span className="text-muted tnum mt-1 block font-mono text-[10px]">
                    {formatMessageTime(message.createdAt)}
                  </span>
                </div>
              ))}
            </div>
          </div>
        )}
      </SheetBody>

      {lead.status !== 'qualified' && (
        <SheetFooter>
          <Button onClick={onQualify}>
            <CheckCircle2 aria-hidden className="size-4" />
            Marchează ca calificat
          </Button>
        </SheetFooter>
      )}
    </>
  )
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div className="bg-surface p-4">
      <dt className="text-muted font-mono text-[10px] tracking-widest uppercase">
        {label}
      </dt>
      <dd className="tnum mt-1.5 text-[13px]">{value}</dd>
    </div>
  )
}

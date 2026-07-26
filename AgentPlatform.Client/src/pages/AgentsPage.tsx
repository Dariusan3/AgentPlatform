import { useState } from 'react'
import type { FormEvent } from 'react'
import { Bot, Plus, Settings2, Trash2 } from 'lucide-react'
import { PageHeader } from '@/components/layout/PageHeader'
import { EmptyCard, ErrorCard, LoadingCard } from '@/components/QueryState'
import { Spinner } from '@/components/Spinner'
import { Avatar } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Field, Input, Select, Textarea } from '@/components/ui/field'
import { Switch } from '@/components/ui/switch'
import { languageLabels, languages, toneLabels, tones } from '@/lib/labels'
import {
  useAgents,
  useCreateAgent,
  useDeleteAgent,
  useToggleAgent,
  useUpdateAgent,
} from '@/lib/queries/useAgents'
import type { AgentLanguage, AgentTone, AiAgent, AiAgentInput } from '@/lib/types'

export function AgentsPage() {
  const { data, isPending, isError, error, refetch } = useAgents()
  const toggleAgent = useToggleAgent()
  const deleteAgent = useDeleteAgent()
  const [editing, setEditing] = useState<AiAgent | null>(null)
  const [dialogOpen, setDialogOpen] = useState(false)

  const openNew = () => {
    setEditing(null)
    setDialogOpen(true)
  }

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <PageHeader
        title="Agenții mei AI"
        count={data?.length}
        description="Fiecare agent are propria personalitate, propriul număr de WhatsApp și propriul set de listări."
        action={
          <Button size="md" onClick={openNew}>
            <Plus aria-hidden className="size-4" />
            Agent nou
          </Button>
        }
      />

      {isPending ? (
        <LoadingCard label="Se încarcă agenții…" />
      ) : isError ? (
        <ErrorCard error={error} onRetry={() => void refetch()} />
      ) : data.length === 0 ? (
        <EmptyCard
          icon={<Bot aria-hidden className="size-6" />}
          title="Încă niciun agent AI"
          description="Creează primul agent, dă-i o personalitate și conectează-i un număr de WhatsApp."
          action={
            <Button size="md" onClick={openNew}>
              <Plus aria-hidden className="size-4" />
              Agent nou
            </Button>
          }
        />
      ) : (
        <div className="grid gap-4 lg:grid-cols-2">
          {data.map((agent) => (
            <Card key={agent.id} className="flex flex-col p-5">
              <div className="flex items-start gap-3.5">
                <Avatar name={agent.name} tinted className="size-10 text-[12px]" />

                <div className="min-w-0 flex-1">
                  <div className="flex items-start justify-between gap-3">
                    <h3 className="text-[14.5px] leading-snug font-medium">
                      {agent.name}
                    </h3>
                    <Badge tone={agent.isActive ? 'success' : 'muted'}>
                      {agent.isActive ? 'activ' : 'inactiv'}
                    </Badge>
                  </div>
                  <p className="text-muted tnum mt-1 font-mono text-[11px]">
                    {agent.whatsAppNumber ?? 'fără număr conectat'}
                  </p>
                </div>
              </div>

              <p className="text-muted mt-4 line-clamp-2 min-h-10 text-[13px] leading-relaxed">
                {agent.persona || 'Fără instrucțiuni încă.'}
              </p>

              <dl className="border-line mt-4 flex gap-6 border-t pt-4">
                <div>
                  <dt className="text-muted font-mono text-[10px] tracking-widest uppercase">
                    conversații
                  </dt>
                  <dd className="tnum mt-1 text-[15px] font-medium">
                    {agent.conversationsCount}
                  </dd>
                </div>
                <div>
                  <dt className="text-muted font-mono text-[10px] tracking-widest uppercase">
                    leaduri
                  </dt>
                  <dd className="tnum mt-1 text-[15px] font-medium">
                    {agent.leadsCount}
                  </dd>
                </div>
                <div className="ml-auto flex items-center gap-2.5">
                  <span className="text-muted font-mono text-[10px] tracking-widest uppercase">
                    {agent.isActive ? 'pornit' : 'oprit'}
                  </span>
                  <Switch
                    checked={agent.isActive}
                    disabled={toggleAgent.isPending}
                    onCheckedChange={() => toggleAgent.mutate(agent)}
                    aria-label={`Activează ${agent.name}`}
                  />
                </div>
              </dl>

              <div className="mt-5 flex flex-wrap gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => {
                    setEditing(agent)
                    setDialogOpen(true)
                  }}
                >
                  <Settings2 aria-hidden className="size-3.5" />
                  Configurează
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  className="hover:text-danger ml-auto"
                  disabled={deleteAgent.isPending}
                  onClick={() => deleteAgent.mutate(agent)}
                >
                  <Trash2 aria-hidden className="size-3.5" />
                  Șterge
                </Button>
              </div>
            </Card>
          ))}
        </div>
      )}

      <AgentDialog
        key={editing?.id ?? 'new'}
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        agent={editing}
      />
    </div>
  )
}

function AgentDialog({
  open,
  onOpenChange,
  agent,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  agent: AiAgent | null
}) {
  const createAgent = useCreateAgent()
  const updateAgent = useUpdateAgent()
  const busy = createAgent.isPending || updateAgent.isPending

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (busy) return

    const form = new FormData(event.currentTarget)
    const phone = String(form.get('whatsapp') ?? '').trim()

    const input: AiAgentInput = {
      name: String(form.get('name') ?? '').trim(),
      persona: String(form.get('persona') ?? '').trim() || null,
      tone: String(form.get('tone') ?? 'professional') as AgentTone,
      language: String(form.get('language') ?? 'ro') as AgentLanguage,
      whatsAppNumber: phone ? `+40 ${phone}` : null,
      // La update trimitem starea curenta: comutarea se face din card
      ...(agent ? { isActive: agent.isActive } : {}),
    }

    try {
      if (agent) {
        await updateAgent.mutateAsync({ id: agent.id, input })
      } else {
        await createAgent.mutateAsync(input)
      }
      onOpenChange(false)
    } catch {
      // Mesajul e afisat de hook prin toast
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-xl">
        <DialogHeader>
          <DialogTitle>
            {agent ? 'Configurează agentul' : 'Agent nou'}
          </DialogTitle>
          <DialogDescription>
            Instrucțiunile decid cum vorbește agentul. Scrie-le la persoana a
            doua, ca și cum ai instrui un coleg nou.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="flex min-h-0 flex-1 flex-col">
          <DialogBody className="space-y-4">
            <Field label="Nume agent" htmlFor="name">
              <Input
                id="name"
                name="name"
                defaultValue={agent?.name}
                placeholder="Maria — Apartamente Cluj"
                required
              />
            </Field>

            <Field
              label="Persona și instrucțiuni"
              htmlFor="persona"
              hint="Include limitele: ce nu are voie să promită, ce trebuie să întrebe mereu."
            >
              <Textarea
                id="persona"
                name="persona"
                rows={6}
                defaultValue={agent?.persona}
                placeholder="Ex: Ești Maria, agent imobiliar specializat în apartamente în Cluj. Ești prietenoasă, profesională, întrebi bugetul în primele două mesaje și nu promiți niciodată prețuri sub cele din listare."
              />
            </Field>

            <div className="grid gap-4 sm:grid-cols-2">
              <Field label="Ton" htmlFor="tone">
                <Select
                  id="tone"
                  name="tone"
                  defaultValue={agent?.tone ?? 'professional'}
                >
                  {tones.map((option) => (
                    <option key={option} value={option}>
                      {toneLabels[option]}
                    </option>
                  ))}
                </Select>
              </Field>

              <Field label="Limbă" htmlFor="language">
                <Select
                  id="language"
                  name="language"
                  defaultValue={agent?.language ?? 'ro'}
                >
                  {languages.map((option) => (
                    <option key={option} value={option}>
                      {languageLabels[option]}
                    </option>
                  ))}
                </Select>
              </Field>
            </div>

            <Field label="Număr WhatsApp" htmlFor="whatsapp">
              <div className="flex items-stretch">
                <span className="border-line-strong text-muted rounded-l-btn tnum grid shrink-0 place-items-center border border-r-0 bg-hover px-3 font-mono text-[13px]">
                  +40
                </span>
                <Input
                  id="whatsapp"
                  name="whatsapp"
                  className="rounded-l-none"
                  defaultValue={agent?.whatsAppNumber?.replace('+40 ', '') ?? ''}
                  placeholder="721 118 204"
                  inputMode="tel"
                />
              </div>
            </Field>
          </DialogBody>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
            >
              Anulează
            </Button>
            <Button type="submit" disabled={busy}>
              {busy && <Spinner className="size-4" />}
              {agent ? 'Salvează' : 'Creează agentul'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

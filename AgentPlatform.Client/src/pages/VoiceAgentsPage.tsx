import { useState } from 'react'
import type { FormEvent } from 'react'
import { PhoneCall, Plus, Settings2, Trash2 } from 'lucide-react'
import { PageHeader } from '@/components/layout/PageHeader'
import { EmptyCard, ErrorCard, LoadingCard } from '@/components/QueryState'
import { Spinner } from '@/components/Spinner'
import { VoiceTestDialog } from '@/components/VoiceTestDialog'
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
import {
  useCreateVoiceAgent,
  useDeleteVoiceAgent,
  useToggleVoiceAgent,
  useUpdateVoiceAgent,
  useVoiceAgents,
} from '@/lib/queries/useVoiceAgents'
import { voiceNames } from '@/lib/types'
import type { VoiceAgent, VoiceAgentInput, VoiceName } from '@/lib/types'

const voiceLabels: Record<VoiceName, string> = {
  'ro-RO-AlinaNeural': 'Alina — voce feminină',
  'ro-RO-EmilNeural': 'Emil — voce masculină',
}

export function VoiceAgentsPage() {
  const { data, isPending, isError, error, refetch } = useVoiceAgents()
  const toggleAgent = useToggleVoiceAgent()
  const deleteAgent = useDeleteVoiceAgent()

  const [editing, setEditing] = useState<VoiceAgent | null>(null)
  const [formOpen, setFormOpen] = useState(false)
  const [testing, setTesting] = useState<VoiceAgent | null>(null)

  const openNew = () => {
    setEditing(null)
    setFormOpen(true)
  }

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <PageHeader
        title="Agenți vocali"
        count={data?.length}
        description="Preiau apelurile telefonice, califică apelantul și programează vizionări. Poți vorbi cu ei direct de aici, fără telefon."
        action={
          <Button size="md" onClick={openNew}>
            <Plus aria-hidden className="size-4" />
            Agent vocal nou
          </Button>
        }
      />

      {isPending ? (
        <LoadingCard label="Se încarcă agenții vocali…" />
      ) : isError ? (
        <ErrorCard error={error} onRetry={() => void refetch()} />
      ) : data.length === 0 ? (
        <EmptyCard
          icon={<PhoneCall aria-hidden className="size-6" />}
          title="Încă niciun agent vocal"
          description="Creează unul, alege-i vocea și scrie-i salutul. Îl poți testa vorbind în microfon, înainte să conectezi vreun număr."
          action={
            <Button size="md" onClick={openNew}>
              <Plus aria-hidden className="size-4" />
              Agent vocal nou
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
                    {agent.twilioPhoneNumber ?? 'fără număr conectat'}
                  </p>
                </div>
              </div>

              <p className="text-muted mt-4 line-clamp-2 min-h-10 text-[13px] leading-relaxed">
                {agent.greetingMessage || 'Fără salut configurat.'}
              </p>

              <dl className="border-line mt-4 flex gap-6 border-t pt-4">
                <div>
                  <dt className="text-muted font-mono text-[10px] tracking-widest uppercase">
                    apeluri
                  </dt>
                  <dd className="tnum mt-1 text-[15px] font-medium">
                    {agent.callsCount}
                  </dd>
                </div>
                <div>
                  <dt className="text-muted font-mono text-[10px] tracking-widest uppercase">
                    leaduri
                  </dt>
                  <dd className="tnum mt-1 text-[15px] font-medium">
                    {agent.qualifiedLeadsCount}
                  </dd>
                </div>
                <div>
                  <dt className="text-muted font-mono text-[10px] tracking-widest uppercase">
                    voce
                  </dt>
                  <dd className="mt-1 text-[13px]">
                    {agent.voiceName.includes('Emil') ? 'Emil' : 'Alina'}
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
                <Button size="sm" disabled={!agent.isActive} onClick={() => setTesting(agent)}>
                  <PhoneCall aria-hidden className="size-3.5" />
                  Vorbește cu el
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => {
                    setEditing(agent)
                    setFormOpen(true)
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

      <VoiceAgentDialog
        key={editing?.id ?? 'new'}
        open={formOpen}
        onOpenChange={setFormOpen}
        agent={editing}
      />

      <VoiceTestDialog
        key={testing?.id ?? 'none'}
        open={testing !== null}
        onOpenChange={(open) => !open && setTesting(null)}
        agent={testing}
      />
    </div>
  )
}

function VoiceAgentDialog({
  open,
  onOpenChange,
  agent,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  agent: VoiceAgent | null
}) {
  const createAgent = useCreateVoiceAgent()
  const updateAgent = useUpdateVoiceAgent()
  const busy = createAgent.isPending || updateAgent.isPending

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (busy) return

    const form = new FormData(event.currentTarget)
    const phone = String(form.get('phone') ?? '').trim()

    const input: VoiceAgentInput = {
      name: String(form.get('name') ?? '').trim(),
      twilioPhoneNumber: phone ? `+40${phone.replace(/\s/g, '')}` : null,
      voiceName: String(form.get('voiceName')) as VoiceName,
      systemPrompt: String(form.get('systemPrompt') ?? '').trim() || null,
      greetingMessage: String(form.get('greetingMessage') ?? '').trim() || null,
      maxCallDurationSeconds: Number(form.get('maxCallDurationSeconds')),
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
            {agent ? 'Configurează agentul vocal' : 'Agent vocal nou'}
          </DialogTitle>
          <DialogDescription>
            La telefon nu există timp de gândire. Scrie instrucțiuni scurte și
            spune-i explicit să pună o singură întrebare pe rând.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="flex min-h-0 flex-1 flex-col">
          <DialogBody className="space-y-4">
            <Field label="Nume agent" htmlFor="name">
              <Input
                id="name"
                name="name"
                defaultValue={agent?.name}
                placeholder="Alina — Apeluri Cluj"
                required
              />
            </Field>

            <Field
              label="Salutul de deschidere"
              htmlFor="greetingMessage"
              hint="Prima replică, rostită imediat ce apelantul intră în linie."
            >
              <Textarea
                id="greetingMessage"
                name="greetingMessage"
                rows={2}
                defaultValue={agent?.greetingMessage}
                placeholder="Bună ziua, ați apelat agenția Portar. Sunt Alina. Cu ce vă pot ajuta?"
              />
            </Field>

            <Field
              label="Instrucțiuni"
              htmlFor="systemPrompt"
              hint="Include ce trebuie să afle mereu: nume, buget, zonă, tip de proprietate."
            >
              <Textarea
                id="systemPrompt"
                name="systemPrompt"
                rows={5}
                defaultValue={agent?.systemPrompt}
                placeholder="Ești Alina, asistentă la agenția Portar. Vorbești scurt și clar, o întrebare pe rând. Afli numele, bugetul și zona, apoi propui o vizionare. Nu inventezi proprietăți care nu sunt în listări."
              />
            </Field>

            <div className="grid gap-4 sm:grid-cols-2">
              <Field label="Voce" htmlFor="voiceName">
                <Select
                  id="voiceName"
                  name="voiceName"
                  defaultValue={agent?.voiceName ?? voiceNames[0]}
                >
                  {voiceNames.map((option) => (
                    <option key={option} value={option}>
                      {voiceLabels[option]}
                    </option>
                  ))}
                </Select>
              </Field>

              <Field
                label="Durată maximă"
                htmlFor="maxCallDurationSeconds"
                hint="În secunde. Limită de cost: un apel uitat deschis consumă credit."
              >
                <Input
                  id="maxCallDurationSeconds"
                  name="maxCallDurationSeconds"
                  type="number"
                  min={30}
                  max={1800}
                  defaultValue={agent?.maxCallDurationSeconds ?? 300}
                  required
                />
              </Field>
            </div>

            <Field
              label="Număr Twilio"
              htmlFor="phone"
              hint="Opțional. Fără număr, agentul se poate testa doar din platformă."
            >
              <div className="flex items-stretch">
                <span className="border-line-strong text-muted rounded-l-btn tnum grid shrink-0 place-items-center border border-r-0 bg-hover px-3 font-mono text-[13px]">
                  +40
                </span>
                <Input
                  id="phone"
                  name="phone"
                  className="rounded-l-none"
                  defaultValue={agent?.twilioPhoneNumber?.replace('+40', '') ?? ''}
                  placeholder="312 345 678"
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

import { useState } from 'react'
import type { FormEvent } from 'react'
import { Info, Send } from 'lucide-react'
import { Spinner } from '@/components/Spinner'
import { Button } from '@/components/ui/button'
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
import { useAgents } from '@/lib/queries/useAgents'
import { useSimulateMessage } from '@/lib/queries/useSimulate'

/**
 * Trimite un mesaj de test catre un agent, ca sa poti verifica raspunsurile fara
 * Twilio si fara telefon. Acelasi drum pe care il va lua webhookul real.
 */
export function SimulateDialog({
  open,
  onOpenChange,
  onSimulated,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSimulated?: (conversationId: string) => void
}) {
  const { data: agents } = useAgents()
  const simulate = useSimulateMessage()
  const [message, setMessage] = useState('')

  const activeAgents = (agents ?? []).filter((agent) => agent.isActive)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (simulate.isPending) return

    const form = new FormData(event.currentTarget)

    try {
      const result = await simulate.mutateAsync({
        aiAgentId: String(form.get('aiAgentId') ?? ''),
        contactPhone: String(form.get('contactPhone') ?? '').trim(),
        contactName: String(form.get('contactName') ?? '').trim() || null,
        message: String(form.get('message') ?? '').trim(),
      })
      onOpenChange(false)
      setMessage('')
      onSimulated?.(result.conversation.id)
    } catch {
      // Mesajul e afisat de hook prin toast
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Testează un agent</DialogTitle>
          <DialogDescription>
            Trimite un mesaj ca și cum ar veni de pe WhatsApp. Agentul răspunde
            folosind persona lui și listările tale.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="flex min-h-0 flex-1 flex-col">
          <DialogBody className="space-y-4">
            {activeAgents.length === 0 ? (
              <p className="text-muted text-[13.5px] leading-relaxed">
                Niciun agent activ. Creează unul în pagina Agenți AI și
                pornește-l — un agent oprit nu răspunde.
              </p>
            ) : (
              <>
                <Field label="Agent" htmlFor="aiAgentId">
                  <Select id="aiAgentId" name="aiAgentId" required>
                    {activeAgents.map((agent) => (
                      <option key={agent.id} value={agent.id}>
                        {agent.name}
                      </option>
                    ))}
                  </Select>
                </Field>

                <div className="grid gap-4 sm:grid-cols-2">
                  <Field label="Telefon contact" htmlFor="contactPhone">
                    <Input
                      id="contactPhone"
                      name="contactPhone"
                      defaultValue="+40 745 000 000"
                      inputMode="tel"
                      required
                    />
                  </Field>
                  <Field label="Nume contact" htmlFor="contactName">
                    <Input
                      id="contactName"
                      name="contactName"
                      placeholder="Andrei Mureșan"
                    />
                  </Field>
                </div>

                <Field
                  label="Mesajul clientului"
                  htmlFor="message"
                  hint="Mesajele către același telefon continuă aceeași conversație."
                >
                  <Textarea
                    id="message"
                    name="message"
                    rows={3}
                    value={message}
                    onChange={(event) => setMessage(event.target.value)}
                    placeholder="Bună seara, mai e disponibil apartamentul din Gheorgheni?"
                    required
                  />
                </Field>

                <p className="text-muted flex items-start gap-2 text-[12px] leading-relaxed">
                  <Info aria-hidden className="mt-px size-3.5 shrink-0" />
                  Simulare locală, fără Twilio. Nu se trimite niciun mesaj real.
                </p>
              </>
            )}
          </DialogBody>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
            >
              Anulează
            </Button>
            <Button
              type="submit"
              disabled={simulate.isPending || activeAgents.length === 0}
            >
              {simulate.isPending ? (
                <Spinner className="size-4" />
              ) : (
                <Send aria-hidden className="size-4" />
              )}
              Trimite mesajul
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

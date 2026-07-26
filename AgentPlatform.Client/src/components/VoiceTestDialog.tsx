import { useEffect, useRef, useState } from 'react'
import { Info, Mic, Phone, PhoneOff } from 'lucide-react'
import { Spinner } from '@/components/Spinner'
import { Badge } from '@/components/ui/badge'
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
import { useStartTestCall, useVoiceCall } from '@/lib/queries/useVoiceAgents'
import { BrowserVoiceCall } from '@/lib/voice/call'
import type { CallState } from '@/lib/voice/call'
import type { VoiceAgent } from '@/lib/types'

const statusLabels: Record<CallState, string> = {
  idle: 'în așteptare',
  connecting: 'se conectează',
  listening: 'te ascult',
  speaking: 'agentul vorbește',
  ended: 'apel încheiat',
  error: 'apelul a eșuat',
}

/**
 * Vorbesti cu agentul vocal direct din browser, fara Twilio si fara numar de
 * telefon. Microfonul trece prin acelasi protocol Media Streams, deci ce auzi
 * aici e exact ce va auzi un client care suna.
 */
export function VoiceTestDialog({
  open,
  onOpenChange,
  agent,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  agent: VoiceAgent | null
}) {
  const startCall = useStartTestCall()
  const [state, setState] = useState<CallState>('idle')
  const [callId, setCallId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const callRef = useRef<BrowserVoiceCall | null>(null)
  const meterRef = useRef<HTMLDivElement>(null)
  const transcriptRef = useRef<HTMLDivElement>(null)

  const live = state === 'connecting' || state === 'listening' || state === 'speaking'
  const { data: call } = useVoiceCall(callId, live)

  // Inchiderea dialogului trebuie sa taie si apelul: altfel microfonul rămâne
  // deschis si agentul continua sa consume tokeni in fundal.
  useEffect(() => {
    if (!open && callRef.current) {
      callRef.current.stop()
      callRef.current = null
    }
  }, [open])

  useEffect(() => () => callRef.current?.stop(), [])

  // Transcrierea creste in jos; fara asta ultima replica ramane sub margine
  useEffect(() => {
    const node = transcriptRef.current
    if (node) node.scrollTop = node.scrollHeight
  }, [call?.messages.length])

  const begin = async () => {
    if (!agent || callRef.current) return

    setError(null)

    try {
      const opened = await startCall.mutateAsync(agent.id)
      setCallId(opened.callId)

      const voiceCall = new BrowserVoiceCall({
        onState: setState,
        onLevel: (level) => {
          // Scris direct in DOM: la 60 de actualizari pe secunda, starea React
          // ar redesena transcrierea la fiecare cadru audio.
          const node = meterRef.current
          if (node) node.style.scale = `${Math.max(0.02, level)} 1`
        },
        onError: setError,
      })

      callRef.current = voiceCall
      await voiceCall.start(opened.streamUrl, opened.callSid)
    } catch {
      // Mesajul vine din toastul mutatiei
    }
  }

  const hangUp = () => {
    callRef.current?.stop()
    callRef.current = null
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>
            Vorbește cu {agent?.name.split('—')[0].trim() ?? 'agentul'}
          </DialogTitle>
          <DialogDescription>
            Apel purtat prin browser, pe același protocol pe care îl folosește
            Twilio. Nu se sună nici un număr real.
          </DialogDescription>
        </DialogHeader>

        <DialogBody className="space-y-4">
          {state === 'idle' ? (
            <div className="space-y-4">
              <p className="text-muted text-[13.5px] leading-relaxed">
                Când pornești apelul, browserul cere accesul la microfon. Agentul
                te salută primul — apoi vorbește normal și lasă o scurtă pauză, ca
                la telefon. Pauza e semnalul că ai terminat replica.
              </p>
              <p className="text-muted flex items-start gap-2 text-[12px] leading-relaxed">
                <Info aria-hidden className="mt-px size-3.5 shrink-0" />
                Folosește căști dacă poți. Pe boxe, agentul se aude uneori pe el
                însuși și își transcrie propriile replici.
              </p>
            </div>
          ) : (
            <>
              <div className="border-line rounded-card border p-4">
                <div className="flex items-center justify-between gap-3">
                  <span className="flex items-center gap-2.5 text-[13.5px]">
                    {state === 'connecting' ? (
                      <Spinner className="size-4" />
                    ) : (
                      <Mic
                        aria-hidden
                        className={
                          state === 'listening'
                            ? 'text-accent size-4'
                            : 'text-muted size-4'
                        }
                      />
                    )}
                    {statusLabels[state]}
                  </span>

                  {call?.leadQualified && <Badge tone="success">lead calificat</Badge>}
                </div>

                <div className="bg-hover mt-3.5 h-1 overflow-hidden rounded-full">
                  <div
                    ref={meterRef}
                    className="bg-accent h-full origin-left"
                    style={{ scale: '0.02 1' }}
                  />
                </div>
              </div>

              {error && (
                <p className="text-danger text-[13px] leading-relaxed">{error}</p>
              )}

              <div
                ref={transcriptRef}
                className="max-h-64 space-y-3 overflow-y-auto"
              >
                {call?.messages.length ? (
                  call.messages.map((message) => (
                    <div
                      key={message.id}
                      className={
                        message.role === 'agent'
                          ? 'bg-hover rounded-card px-3.5 py-2.5'
                          : 'border-line rounded-card border px-3.5 py-2.5'
                      }
                    >
                      <p className="text-muted font-mono text-[10px] tracking-widest uppercase">
                        {message.role === 'agent' ? 'agent' : 'tu'}
                      </p>
                      <p className="mt-1 text-[13.5px] leading-relaxed">
                        {message.content}
                      </p>
                    </div>
                  ))
                ) : (
                  <p className="text-muted text-[13px] leading-relaxed">
                    Transcrierea apare aici, replică cu replică.
                  </p>
                )}
              </div>
            </>
          )}
        </DialogBody>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Închide
          </Button>

          {live ? (
            <Button className="hover:bg-danger" onClick={hangUp}>
              <PhoneOff aria-hidden className="size-4" />
              Termină apelul
            </Button>
          ) : (
            <Button onClick={() => void begin()} disabled={startCall.isPending}>
              {startCall.isPending ? (
                <Spinner className="size-4" />
              ) : (
                <Phone aria-hidden className="size-4" />
              )}
              {state === 'idle' ? 'Pornește apelul' : 'Sună din nou'}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

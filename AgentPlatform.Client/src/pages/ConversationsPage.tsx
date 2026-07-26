import { useMemo, useState } from 'react'
import {
  ArrowLeft,
  Info,
  MessageSquare,
  Search,
  UserPlus,
  XCircle,
} from 'lucide-react'
import { EmptyCard, ErrorCard, LoadingCard } from '@/components/QueryState'
import { Spinner } from '@/components/Spinner'
import { Avatar } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/field'
import { Progress } from '@/components/ui/progress'
import { conversationStatusLabels, formatMessageTime } from '@/lib/labels'
import {
  useCloseConversation,
  useConversation,
  useConversations,
  useConvertConversation,
} from '@/lib/queries/useConversations'
import { conversationTone, scoreClass } from '@/lib/tones'
import type { Conversation } from '@/lib/types'
import { cn } from '@/lib/utils'

export function ConversationsPage() {
  const [activeId, setActiveId] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  // Pe mobil nu incap ambele coloane, deci lista si conversatia se schimba
  const [showThread, setShowThread] = useState(false)

  const { data, isPending, isError, error, refetch } = useConversations()

  const visible = useMemo(() => {
    const items = data ?? []
    const needle = search.trim().toLowerCase()
    if (!needle) return items

    return items.filter((item) =>
      `${item.contactName ?? ''} ${item.contactPhone} ${item.messages.at(-1)?.content ?? ''}`
        .toLowerCase()
        .includes(needle),
    )
  }, [data, search])

  // Prima conversatie e selectata implicit pe desktop
  const selectedId = activeId ?? visible[0]?.id ?? null

  if (isPending) {
    return (
      <div className="mx-auto max-w-6xl">
        <LoadingCard label="Se încarcă conversațiile…" />
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

  if (data.length === 0) {
    return (
      <div className="mx-auto max-w-6xl">
        <EmptyCard
          icon={<MessageSquare aria-hidden className="size-6" />}
          title="Încă nicio conversație"
          description="Conversațiile apar automat aici când un client scrie pe numărul de WhatsApp conectat la un agent activ."
        />
      </div>
    )
  }

  return (
    <div className="border-line bg-surface rounded-card mx-auto flex h-[calc(100svh-8.5rem)] max-w-6xl overflow-hidden border">
      <div
        className={cn(
          'border-line flex w-full shrink-0 flex-col lg:w-[22rem] lg:border-r',
          showThread && 'hidden lg:flex',
        )}
      >
        <div className="border-line shrink-0 border-b p-3">
          <div className="relative">
            <Search
              aria-hidden
              className="text-muted pointer-events-none absolute top-1/2 left-3.5 size-3.5 -translate-y-1/2"
            />
            <Input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Caută în conversații"
              aria-label="Caută în conversații"
              className="h-9 pl-9 text-[13px]"
            />
          </div>
        </div>

        <div className="min-h-0 flex-1 overflow-y-auto">
          {visible.length === 0 ? (
            <p className="text-muted p-5 text-[13px]">
              Nicio conversație pentru „{search}".
            </p>
          ) : (
            visible.map((conversation) => (
              <button
                key={conversation.id}
                type="button"
                onClick={() => {
                  setActiveId(conversation.id)
                  setShowThread(true)
                }}
                className={cn(
                  'border-line flex w-full items-start gap-3 border-b p-3.5 text-left transition-colors',
                  conversation.id === selectedId ? 'bg-raised' : 'hover:bg-hover',
                )}
              >
                <Avatar
                  name={conversation.contactName ?? conversation.contactPhone}
                  className="mt-0.5"
                />
                <span className="min-w-0 flex-1">
                  <span className="flex items-baseline justify-between gap-2">
                    <span className="truncate text-[13.5px] font-medium">
                      {conversation.contactName ?? conversation.contactPhone}
                    </span>
                    <span className="text-muted tnum shrink-0 font-mono text-[10.5px]">
                      {conversation.lastMessageAt
                        ? formatMessageTime(conversation.lastMessageAt)
                        : ''}
                    </span>
                  </span>
                  <span className="text-muted mt-1 block truncate text-[12.5px]">
                    {conversation.messages.at(-1)?.content ?? 'Fără mesaje'}
                  </span>
                  <span className="mt-2 block">
                    <Badge tone={conversationTone[conversation.status]}>
                      {conversationStatusLabels[conversation.status]}
                    </Badge>
                  </span>
                </span>
              </button>
            ))
          )}
        </div>
      </div>

      <div
        className={cn(
          'flex min-w-0 flex-1 flex-col',
          !showThread && 'hidden lg:flex',
        )}
      >
        {selectedId ? (
          <Thread id={selectedId} onBack={() => setShowThread(false)} />
        ) : (
          <div className="text-muted grid flex-1 place-items-center p-6 text-[13.5px]">
            Alege o conversație din stânga.
          </div>
        )}
      </div>
    </div>
  )
}

/** Firul complet vine din endpointul de detaliu: lista are doar ultimul mesaj. */
function Thread({ id, onBack }: { id: string; onBack: () => void }) {
  const { data, isPending, isError, error, refetch } = useConversation(id)

  if (isPending) {
    return (
      <div className="grid flex-1 place-items-center">
        <Spinner className="size-5" />
      </div>
    )
  }

  if (isError) {
    return (
      <div className="p-5">
        <ErrorCard error={error} onRetry={() => void refetch()} />
      </div>
    )
  }

  return <ThreadContent conversation={data} onBack={onBack} />
}

function ThreadContent({
  conversation,
  onBack,
}: {
  conversation: Conversation
  onBack: () => void
}) {
  const closeConversation = useCloseConversation()
  const convertConversation = useConvertConversation()

  const alreadyLead = conversation.lead !== null
  const isClosed = conversation.status === 'closed'

  return (
    <>
      <div className="border-line flex shrink-0 items-center gap-3 border-b p-3.5">
        <button
          type="button"
          onClick={onBack}
          aria-label="Înapoi la listă"
          className="text-muted hover:text-fg hover:bg-hover rounded-btn grid size-8 place-items-center transition-colors lg:hidden"
        >
          <ArrowLeft aria-hidden className="size-4" />
        </button>

        <Avatar
          name={conversation.contactName ?? conversation.contactPhone}
          tinted
        />
        <div className="min-w-0 flex-1">
          <p className="truncate text-[14px] font-medium">
            {conversation.contactName ?? conversation.contactPhone}
          </p>
          <p className="text-muted tnum font-mono text-[10.5px]">
            {conversation.contactPhone}
          </p>
        </div>
        <Badge tone="success" className="capitalize">
          {conversation.channel}
        </Badge>
      </div>

      <div className="min-h-0 flex-1 space-y-3 overflow-y-auto p-4 sm:p-5">
        {conversation.messages.length === 0 ? (
          <p className="text-muted text-center text-[13px]">
            Conversația nu are încă mesaje.
          </p>
        ) : (
          conversation.messages.map((message) => (
            <div
              key={message.id}
              className={cn(
                'max-w-[85%] sm:max-w-[70%]',
                message.role !== 'user' && 'ml-auto',
              )}
            >
              <div
                className={cn(
                  'px-3.5 py-2.5 text-[13.5px] leading-relaxed',
                  message.role === 'user'
                    ? 'bg-raised rounded-[12px] rounded-bl-[3px]'
                    : 'rounded-[12px] rounded-br-[3px] bg-[#1f1a0f]',
                )}
              >
                {message.content}
              </div>
              <p
                className={cn(
                  'text-muted tnum mt-1 font-mono text-[10px]',
                  message.role !== 'user' && 'text-right',
                )}
              >
                {formatMessageTime(message.createdAt)}
              </p>
            </div>
          ))
        )}
      </div>

      <div className="border-line shrink-0 border-t p-3.5">
        <div className="flex flex-wrap items-center gap-3">
          <div className="flex min-w-40 flex-1 items-center gap-2.5">
            <span className="text-muted shrink-0 font-mono text-[10px] tracking-widest uppercase">
              lead score
            </span>
            <Progress
              value={conversation.leadScore}
              indicatorClassName={scoreClass(conversation.leadScore)}
            />
            <span className="tnum shrink-0 font-mono text-[11px]">
              {conversation.leadScore}
            </span>
          </div>

          <div className="flex gap-2">
            {!isClosed && (
              <Button
                size="sm"
                variant="ghost"
                disabled={closeConversation.isPending}
                onClick={() => closeConversation.mutate(conversation.id)}
              >
                <XCircle aria-hidden className="size-3.5" />
                Închide
              </Button>
            )}
            <Button
              size="sm"
              variant="outline"
              disabled={alreadyLead || convertConversation.isPending}
              onClick={() => convertConversation.mutate(conversation.id)}
            >
              {convertConversation.isPending ? (
                <Spinner className="size-3.5" />
              ) : (
                <UserPlus aria-hidden className="size-3.5" />
              )}
              {alreadyLead ? 'Deja lead' : 'Marchează ca lead'}
            </Button>
          </div>
        </div>

        <p className="text-muted mt-3 flex items-start gap-2 text-[11.5px] leading-relaxed">
          <Info aria-hidden className="mt-px size-3.5 shrink-0" />
          Doar citire. Agentul AI duce conversația singur — intervii doar dacă îl
          oprești din pagina Agenți.
        </p>
      </div>
    </>
  )
}

import { useState } from 'react'
import type { FormEvent } from 'react'
import { ArrowDown, ArrowUp, Copy, RefreshCw, Upload } from 'lucide-react'
import { toast } from 'sonner'
import { ErrorCard, LoadingCard } from '@/components/QueryState'
import { Spinner } from '@/components/Spinner'
import { Avatar } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardBody, CardHeader, CardTitle } from '@/components/ui/card'
import { Field, Input } from '@/components/ui/field'
import { Progress } from '@/components/ui/progress'
import { Switch } from '@/components/ui/switch'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { formatNumber } from '@/lib/labels'
import {
  useProfile,
  useUpdatePassword,
  useUpdateProfile,
  useUsage,
} from '@/lib/queries/useSettings'
import type { Profile, Usage } from '@/lib/types'

/**
 * Prețurile sunt informatie de marketing, nu date de cont: nu exista integrare
 * de facturare, deci nu are rost sa vina din API.
 */
const planPrices: Record<string, number> = {
  starter: 99,
  pro: 249,
  agency: 599,
}

const planLabels: Record<string, string> = {
  starter: 'Starter',
  pro: 'Pro',
  agency: 'Agency',
}

export function SettingsPage() {
  return (
    <div className="mx-auto max-w-3xl">
      <Tabs defaultValue="cont">
        <TabsList>
          <TabsTrigger value="cont">Cont</TabsTrigger>
          <TabsTrigger value="plan">Plan și facturare</TabsTrigger>
          <TabsTrigger value="notificari">Notificări</TabsTrigger>
          <TabsTrigger value="api">API</TabsTrigger>
        </TabsList>

        <TabsContent value="cont">
          <AccountTab />
        </TabsContent>
        <TabsContent value="plan">
          <PlanTab />
        </TabsContent>
        <TabsContent value="notificari">
          <NotificationsTab />
        </TabsContent>
        <TabsContent value="api">
          <ApiTab />
        </TabsContent>
      </Tabs>
    </div>
  )
}

function AccountTab() {
  const { data, isPending, isError, error, refetch } = useProfile()

  if (isPending) return <LoadingCard label="Se încarcă profilul…" />
  if (isError) return <ErrorCard error={error} onRetry={() => void refetch()} />

  return <AccountForms profile={data} />
}

function AccountForms({ profile }: { profile: Profile }) {
  const updateProfile = useUpdateProfile()
  const updatePassword = useUpdatePassword()

  const saveProfile = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const form = new FormData(event.currentTarget)

    updateProfile.mutate({
      fullName: String(form.get('fullName') ?? '').trim() || null,
      companyName: String(form.get('companyName') ?? '').trim() || null,
      phone: String(form.get('phone') ?? '').trim() || null,
    })
  }

  const changePassword = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const form = event.currentTarget
    const data = new FormData(form)
    const next = String(data.get('newPassword') ?? '')
    const confirm = String(data.get('confirmNewPassword') ?? '')

    if (next.length < 6) {
      toast.error('Parola trebuie să aibă minim 6 caractere.')
      return
    }
    if (next !== confirm) {
      toast.error('Parolele nu se potrivesc.')
      return
    }

    updatePassword.mutate(next, { onSuccess: () => form.reset() })
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Datele contului</CardTitle>
        </CardHeader>
        <CardBody>
          <div className="flex items-center gap-4">
            <Avatar
              name={profile.fullName || profile.email}
              tinted
              className="size-14 text-[16px]"
            />
            <div>
              <Button variant="outline" size="sm" disabled>
                <Upload aria-hidden className="size-3.5" />
                Schimbă poza
              </Button>
              <p className="text-muted mt-2 text-[12px]">
                Încărcarea de imagini nu e încă disponibilă.
              </p>
            </div>
          </div>

          <form onSubmit={saveProfile} className="mt-6 space-y-4">
            <div className="grid gap-4 sm:grid-cols-2">
              <Field label="Nume complet" htmlFor="fullName">
                <Input
                  id="fullName"
                  name="fullName"
                  defaultValue={profile.fullName ?? ''}
                />
              </Field>

              <Field
                label="Email"
                htmlFor="email"
                hint="Adresa de login nu se poate schimba de aici."
              >
                <Input
                  id="email"
                  value={profile.email}
                  readOnly
                  className="text-muted cursor-not-allowed"
                />
              </Field>

              <Field label="Telefon" htmlFor="phone">
                <Input
                  id="phone"
                  name="phone"
                  inputMode="tel"
                  defaultValue={profile.phone ?? ''}
                  placeholder="+40 721 000 000"
                />
              </Field>

              <Field label="Companie" htmlFor="companyName">
                <Input
                  id="companyName"
                  name="companyName"
                  defaultValue={profile.companyName ?? ''}
                  placeholder="Agenția Ta SRL"
                />
              </Field>
            </div>

            <div className="flex justify-end pt-2">
              <Button type="submit" disabled={updateProfile.isPending}>
                {updateProfile.isPending && <Spinner className="size-4" />}
                Salvează modificările
              </Button>
            </div>
          </form>
        </CardBody>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Schimbă parola</CardTitle>
        </CardHeader>
        <CardBody>
          <form onSubmit={changePassword} className="space-y-4">
            <div className="grid gap-4 sm:grid-cols-2">
              <Field label="Parola nouă" htmlFor="newPassword">
                <Input
                  id="newPassword"
                  name="newPassword"
                  type="password"
                  autoComplete="new-password"
                  placeholder="••••••••"
                />
              </Field>
              <Field label="Confirmă parola nouă" htmlFor="confirmNewPassword">
                <Input
                  id="confirmNewPassword"
                  name="confirmNewPassword"
                  type="password"
                  autoComplete="new-password"
                  placeholder="••••••••"
                />
              </Field>
            </div>
            <p className="text-muted text-[12px] leading-relaxed">
              Parola se schimbă prin Supabase Auth, de aceea nu îți cerem cea
              veche — sesiunea curentă e deja dovada identității.
            </p>
            <div className="flex justify-end pt-2">
              <Button
                type="submit"
                variant="outline"
                disabled={updatePassword.isPending}
              >
                {updatePassword.isPending && <Spinner className="size-4" />}
                Schimbă parola
              </Button>
            </div>
          </form>
        </CardBody>
      </Card>
    </div>
  )
}

function PlanTab() {
  const { data, isPending, isError, error, refetch } = useUsage()

  if (isPending) return <LoadingCard label="Se încarcă planul…" />
  if (isError) return <ErrorCard error={error} onRetry={() => void refetch()} />

  return <PlanContent usage={data} />
}

function PlanContent({ usage }: { usage: Usage }) {
  const price = planPrices[usage.plan] ?? 0
  const label = planLabels[usage.plan] ?? usage.plan

  return (
    <div className="space-y-6">
      <Card>
        <CardBody className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <div className="flex items-center gap-2.5">
              <Badge tone="amber">{label}</Badge>
              <span className="text-muted font-mono text-[11px]">
                planul curent
              </span>
            </div>
            <p className="mt-3 flex items-baseline gap-1.5">
              <span className="tnum text-[28px] leading-none font-medium tracking-[-0.03em]">
                {price}
              </span>
              <span className="text-muted text-[13px]">RON / lună</span>
            </p>
            <p className="text-muted mt-2 text-[12.5px]">
              Facturarea nu e încă activată.
            </p>
          </div>

          <div className="flex flex-col gap-2 sm:items-end">
            <Button
              size="sm"
              disabled
              onClick={() => toast.success('Cerere de upgrade trimisă.')}
            >
              <ArrowUp aria-hidden className="size-3.5" />
              Treci pe Agency
            </Button>
            <Button size="sm" variant="ghost" disabled>
              <ArrowDown aria-hidden className="size-3.5" />
              Treci pe Starter
            </Button>
          </div>
        </CardBody>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Utilizare luna aceasta</CardTitle>
          <span className="text-muted tnum font-mono text-[11px]">
            {usage.month}
          </span>
        </CardHeader>
        <CardBody className="space-y-6">
          <UsageRow
            label="Conversații"
            used={usage.messagesCount}
            limit={usage.messagesLimit}
          />
          <UsageRow
            label="Leaduri generate"
            used={usage.leadsGenerated}
            limit={usage.leadsLimit}
          />
        </CardBody>
      </Card>
    </div>
  )
}

function UsageRow({
  label,
  used,
  limit,
}: {
  label: string
  used: number
  limit: number | null
}) {
  // `null` inseamna nelimitat: o bara de progres n-ar avea ce sa reprezinte
  const percent = limit ? Math.min(Math.round((used / limit) * 100), 100) : null

  return (
    <div>
      <div className="flex items-baseline justify-between gap-4">
        <span className="text-[13.5px]">{label}</span>
        <span className="tnum text-muted font-mono text-[12px]">
          {formatNumber(used)} / {limit ? formatNumber(limit) : '∞'}
        </span>
      </div>

      {percent === null ? (
        <p className="text-muted mt-3 text-[12px]">
          Nelimitat pe planul tău.
        </p>
      ) : (
        <>
          <Progress
            value={percent}
            className="mt-3"
            indicatorClassName={percent >= 80 ? 'bg-danger' : 'bg-amber'}
          />
          <p className="text-muted mt-2 text-[12px]">
            {percent >= 80
              ? 'Aproape de limită. Trecerea pe Agency o ridică imediat.'
              : `${percent}% din limita lunară.`}
          </p>
        </>
      )}
    </div>
  )
}

const notificationRows = [
  {
    id: 'lead-nou',
    title: 'Lead nou generat',
    description: 'Când un agent califică un contact.',
    channels: ['Email', 'Push'],
  },
  {
    id: 'conversatie-noua',
    title: 'Conversație nouă',
    description: 'La primul mesaj de la un număr necunoscut.',
    channels: ['Push'],
  },
  {
    id: 'raport',
    title: 'Raport săptămânal',
    description: 'Sinteza de luni dimineață.',
    channels: ['Email'],
  },
  {
    id: 'utilizare',
    title: 'Alertă la 80% din limită',
    description: 'Ca să nu te prindă nepregătit la finalul lunii.',
    channels: ['Email'],
  },
]

function NotificationsTab() {
  const [enabled, setEnabled] = useState<Record<string, boolean>>({
    'lead-nou': true,
    'conversatie-noua': true,
    raport: true,
    utilizare: false,
  })

  return (
    <Card>
      <CardHeader>
        <CardTitle>Ce vrei să afli</CardTitle>
        <Badge tone="outline">local</Badge>
      </CardHeader>
      <div>
        {notificationRows.map((row) => (
          <div
            key={row.id}
            className="border-line flex items-start justify-between gap-6 border-b px-5 py-4 last:border-b-0"
          >
            <div className="min-w-0">
              <p className="text-[13.5px] font-medium">{row.title}</p>
              <p className="text-muted mt-1 text-[12.5px] leading-relaxed">
                {row.description}
              </p>
              <div className="mt-2 flex gap-1.5">
                {row.channels.map((channel) => (
                  <Badge key={channel} tone="outline">
                    {channel}
                  </Badge>
                ))}
              </div>
            </div>
            <Switch
              checked={enabled[row.id] ?? false}
              onCheckedChange={(value) =>
                setEnabled((current) => ({ ...current, [row.id]: value }))
              }
              aria-label={row.title}
              className="mt-1 shrink-0"
            />
          </div>
        ))}
      </div>
      <CardBody className="border-line border-t">
        <p className="text-muted text-[12px] leading-relaxed">
          Preferințele nu se salvează încă pe server — backendul nu are endpoint
          de notificări. Se resetează la reîncărcarea paginii.
        </p>
      </CardBody>
    </Card>
  )
}

function ApiTab() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Cheie API</CardTitle>
        <Badge tone="outline">în lucru</Badge>
      </CardHeader>
      <CardBody className="space-y-4">
        <p className="text-muted text-[13px] leading-relaxed">
          Cheile API nu sunt încă implementate în backend. Când vor fi, le vei
          putea genera și revoca de aici.
        </p>

        <div className="flex flex-col gap-2 sm:flex-row">
          <Input
            readOnly
            disabled
            value="pk_live_••••••••••••••••••"
            aria-label="Cheia API"
            className="tnum flex-1 font-mono text-[12.5px]"
          />
          <div className="flex gap-2">
            <Button variant="outline" disabled>
              <Copy aria-hidden className="size-3.5" />
              Copiază
            </Button>
            <Button variant="ghost" disabled>
              <RefreshCw aria-hidden className="size-3.5" />
              Regenerează
            </Button>
          </div>
        </div>
      </CardBody>
    </Card>
  )
}

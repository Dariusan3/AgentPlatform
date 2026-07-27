import { useEffect, useState } from 'react'
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
  useNotificationPreferences,
  usePushConfig,
  useSendTestPush,
  useUpdateNotificationPreference,
} from '@/lib/queries/useNotifications'
import { currentStatus, subscribe, unsubscribe } from '@/lib/push'
import type { PushStatus } from '@/lib/push'
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

const groupLabels: Record<string, string> = {
  activitate: 'Activitate',
  apeluri: 'Apeluri vocale',
  operational: 'Operațional',
}

function NotificationsTab() {
  const { data, isPending, isError, error, refetch } = useNotificationPreferences()
  const { data: pushConfig } = usePushConfig()
  const updatePreference = useUpdateNotificationPreference()
  const sendTest = useSendTestPush()

  const [pushStatus, setPushStatus] = useState<PushStatus>('unsupported')
  const [pushBusy, setPushBusy] = useState(false)

  useEffect(() => {
    void currentStatus().then(setPushStatus)
  }, [])

  const togglePush = async () => {
    if (!pushConfig?.publicKey) return
    setPushBusy(true)

    try {
      if (pushStatus === 'subscribed') {
        await unsubscribe()
        toast.success('Notificările de sistem au fost oprite.')
      } else {
        await subscribe(pushConfig.publicKey)
        toast.success('Browserul primește acum notificări.')
      }
      setPushStatus(await currentStatus())
    } catch (pushError) {
      toast.error(
        pushError instanceof Error
          ? pushError.message
          : 'Abonarea la notificări a eșuat.',
      )
    } finally {
      setPushBusy(false)
    }
  }

  if (isPending) return <LoadingCard label="Se încarcă preferințele…" />
  if (isError) return <ErrorCard error={error} onRetry={() => void refetch()} />

  const groups = Object.keys(groupLabels).filter((group) =>
    data.some((preference) => preference.group === group),
  )

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Notificări în browser</CardTitle>
          {pushStatus === 'subscribed' && <Badge tone="success">pornite</Badge>}
        </CardHeader>
        <CardBody className="space-y-4">
          <p className="text-muted text-[13px] leading-relaxed">
            {pushStatus === 'unsupported'
              ? 'Browserul acesta nu acceptă notificări de sistem. Cele din aplicație funcționează în continuare.'
              : pushStatus === 'denied'
                ? 'Ai refuzat notificările pentru acest site. Reactivează-le din setările site-ului, de lângă bara de adresă.'
                : pushStatus === 'subscribed'
                  ? 'Primești notificări chiar și cu tabul închis, atât timp cât browserul rulează.'
                  : 'Pornește-le ca să afli de leaduri și apeluri fără să ții aplicația deschisă.'}
          </p>

          {!pushConfig?.enabled && (
            <p className="text-muted text-[12px] leading-relaxed">
              Serverul nu are chei VAPID configurate, deci abonarea nu ar avea unde
              să trimită. Adaugă secțiunea <code>WebPush</code> în
              appsettings.Development.json.
            </p>
          )}

          <div className="flex flex-wrap gap-2">
            <Button
              size="sm"
              variant={pushStatus === 'subscribed' ? 'outline' : 'primary'}
              disabled={
                pushBusy ||
                !pushConfig?.enabled ||
                pushStatus === 'unsupported' ||
                pushStatus === 'denied'
              }
              onClick={() => void togglePush()}
            >
              {pushBusy && <Spinner className="size-4" />}
              {pushStatus === 'subscribed'
                ? 'Oprește notificările'
                : 'Pornește notificările'}
            </Button>

            {pushStatus === 'subscribed' && (
              <Button
                size="sm"
                variant="outline"
                disabled={sendTest.isPending}
                onClick={() => sendTest.mutate()}
              >
                {sendTest.isPending && <Spinner className="size-4" />}
                Trimite o probă
              </Button>
            )}
          </div>
        </CardBody>
      </Card>

      {groups.map((group) => (
        <Card key={group}>
          <CardHeader>
            <CardTitle>{groupLabels[group]}</CardTitle>
            <div className="text-muted flex gap-6 font-mono text-[10px] tracking-widest uppercase">
              <span>în aplicație</span>
              <span>push</span>
            </div>
          </CardHeader>
          <div>
            {data
              .filter((preference) => preference.group === group)
              .map((preference) => (
                <div
                  key={preference.type}
                  className="border-line flex items-start justify-between gap-6 border-b px-5 py-4 last:border-b-0"
                >
                  <div className="min-w-0">
                    <p className="text-[13.5px] font-medium">{preference.title}</p>
                    <p className="text-muted mt-1 text-[12.5px] leading-relaxed">
                      {preference.description}
                    </p>
                  </div>

                  <div className="flex shrink-0 gap-6 pt-1">
                    <Switch
                      checked={preference.inApp}
                      disabled={updatePreference.isPending}
                      onCheckedChange={(value) =>
                        updatePreference.mutate({
                          type: preference.type,
                          inApp: value,
                          push: preference.push,
                        })
                      }
                      aria-label={`${preference.title} în aplicație`}
                    />
                    <Switch
                      checked={preference.push}
                      disabled={
                        updatePreference.isPending || pushStatus !== 'subscribed'
                      }
                      onCheckedChange={(value) =>
                        updatePreference.mutate({
                          type: preference.type,
                          inApp: preference.inApp,
                          push: value,
                        })
                      }
                      aria-label={`${preference.title} prin push`}
                    />
                  </div>
                </div>
              ))}
          </div>
        </Card>
      ))}
    </div>
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

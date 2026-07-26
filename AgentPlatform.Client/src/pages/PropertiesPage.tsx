import { useMemo, useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import {
  Building2,
  ExternalLink,
  Pencil,
  Plus,
  Search,
  Trash2,
} from 'lucide-react'
import { PageHeader } from '@/components/layout/PageHeader'
import { EmptyCard, ErrorCard, LoadingCard } from '@/components/QueryState'
import { Spinner } from '@/components/Spinner'
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
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import {
  formatEur,
  formatRon,
  propertyTypeLabels,
  propertyTypes,
} from '@/lib/labels'
import { useAgents } from '@/lib/queries/useAgents'
import {
  useCreateProperty,
  useDeleteProperty,
  useProperties,
  useUpdateProperty,
} from '@/lib/queries/useProperties'
import type { Property, PropertyInput, PropertyType } from '@/lib/types'

/** Cursul e doar pentru previzualizarea din formular; backendul recalculeaza. */
const EUR_TO_RON = 4.97

type SortOrder = 'asc' | 'desc'

export function PropertiesPage() {
  const [search, setSearch] = useState('')
  const [type, setType] = useState('')
  const [city, setCity] = useState('')
  const [sort, setSort] = useState<SortOrder>('asc')
  const [editing, setEditing] = useState<Property | null>(null)
  const [dialogOpen, setDialogOpen] = useState(false)

  // Filtrarea si căutarea se fac pe server; sortarea, local
  const { data, isPending, isError, error, refetch, isFetching } = useProperties({
    search,
    type,
    city,
  })
  const deleteProperty = useDeleteProperty()

  const items = useMemo(
    () =>
      [...(data ?? [])].sort((a, b) =>
        sort === 'asc' ? a.priceEur - b.priceEur : b.priceEur - a.priceEur,
      ),
    [data, sort],
  )

  // Orasele vin din datele reale, nu dintr-o lista fixa
  const cities = useMemo(
    () => [...new Set((data ?? []).map((item) => item.city))].sort(),
    [data],
  )

  const hasFilters = Boolean(search || type || city)

  const openNew = () => {
    setEditing(null)
    setDialogOpen(true)
  }

  const openEdit = (property: Property) => {
    setEditing(property)
    setDialogOpen(true)
  }

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <PageHeader
        title="Proprietățile mele"
        count={data?.length}
        description="Listările pe care agenții AI le folosesc când răspund la întrebări despre disponibilitate și preț."
        action={
          <Button size="md" onClick={openNew}>
            <Plus aria-hidden className="size-4" />
            Adaugă proprietate
          </Button>
        }
      />

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <div className="relative sm:col-span-2 lg:col-span-1">
          <Search
            aria-hidden
            className="text-muted pointer-events-none absolute top-1/2 left-3.5 size-3.5 -translate-y-1/2"
          />
          <Input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Caută după titlu sau zonă"
            aria-label="Caută proprietăți"
            className="pl-9"
          />
          {isFetching && !isPending && (
            <Spinner className="absolute top-1/2 right-3 size-3.5 -translate-y-1/2" />
          )}
        </div>

        <Select
          value={type}
          onChange={(event) => setType(event.target.value)}
          aria-label="Filtrează după tip"
        >
          <option value="">Toate tipurile</option>
          {propertyTypes.map((option) => (
            <option key={option} value={option}>
              {propertyTypeLabels[option]}
            </option>
          ))}
        </Select>

        <Select
          value={city}
          onChange={(event) => setCity(event.target.value)}
          aria-label="Filtrează după oraș"
        >
          <option value="">Toate orașele</option>
          {cities.map((option) => (
            <option key={option} value={option}>
              {option}
            </option>
          ))}
        </Select>

        <Select
          value={sort}
          onChange={(event) => setSort(event.target.value as SortOrder)}
          aria-label="Ordonează după preț"
        >
          <option value="asc">Preț crescător</option>
          <option value="desc">Preț descrescător</option>
        </Select>
      </div>

      {isPending ? (
        <LoadingCard label="Se încarcă proprietățile…" />
      ) : isError ? (
        <ErrorCard error={error} onRetry={() => void refetch()} />
      ) : items.length === 0 ? (
        <EmptyCard
          icon={<Building2 aria-hidden className="size-6" />}
          title={
            hasFilters ? 'Nicio proprietate găsită' : 'Încă nicio proprietate'
          }
          description={
            hasFilters
              ? 'Schimbă filtrele sau adaugă o listare nouă.'
              : 'Adaugă prima listare, ca agenții să aibă cu ce răspunde clienților.'
          }
          action={
            !hasFilters && (
              <Button size="md" onClick={openNew}>
                <Plus aria-hidden className="size-4" />
                Adaugă proprietate
              </Button>
            )
          }
        />
      ) : (
        <div className="grid gap-4 lg:grid-cols-3">
          {items.map((property) => (
            <Card key={property.id} className="flex flex-col overflow-hidden">
              {/* Placeholder de imagine: gradient + icon, fara resurse externe */}
              <div className="border-line relative grid h-36 place-items-center border-b bg-[linear-gradient(140deg,#141414,#0a0a0a)]">
                <Building2 aria-hidden className="text-line-strong size-8" />
                <Badge tone="outline" className="bg-surface absolute top-3 left-3">
                  {propertyTypeLabels[property.propertyType] ??
                    property.propertyType}
                </Badge>
              </div>

              <div className="flex flex-1 flex-col p-5">
                <h3 className="text-[14.5px] leading-snug font-medium">
                  {property.title}
                </h3>
                <p className="text-muted mt-1 font-mono text-[11px]">
                  {[property.neighborhood, property.city]
                    .filter(Boolean)
                    .join(' · ')}
                </p>

                <p className="mt-4 flex items-baseline gap-2">
                  <span className="tnum text-[20px] font-medium tracking-[-0.03em]">
                    {formatEur(property.priceEur)} €
                  </span>
                  <span className="text-muted tnum font-mono text-[11px]">
                    {formatRon(property.priceRon)} RON
                  </span>
                </p>

                <dl className="border-line text-muted mt-4 flex gap-4 border-t pt-4 font-mono text-[11px]">
                  <div>
                    <dt className="sr-only">Suprafață</dt>
                    <dd className="tnum">{property.surfaceSqm} m²</dd>
                  </div>
                  {property.rooms > 0 && (
                    <div>
                      <dt className="sr-only">Camere</dt>
                      <dd className="tnum">{property.rooms} camere</dd>
                    </div>
                  )}
                </dl>

                <div className="mt-4 flex items-center gap-1">
                  <IconAction label="Editează" onClick={() => openEdit(property)}>
                    <Pencil aria-hidden className="size-3.5" />
                  </IconAction>
                  {property.listingUrl && (
                    <IconAction label="Vezi listarea" href={property.listingUrl}>
                      <ExternalLink aria-hidden className="size-3.5" />
                    </IconAction>
                  )}
                  <IconAction
                    label="Șterge"
                    danger
                    disabled={deleteProperty.isPending}
                    onClick={() => deleteProperty.mutate(property)}
                  >
                    <Trash2 aria-hidden className="size-3.5" />
                  </IconAction>
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}

      <PropertyDialog
        key={editing?.id ?? 'new'}
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        property={editing}
      />
    </div>
  )
}

function IconAction({
  label,
  children,
  onClick,
  href,
  danger,
  disabled,
}: {
  label: string
  children: ReactNode
  onClick?: () => void
  href?: string
  danger?: boolean
  disabled?: boolean
}) {
  const className = `grid size-8 place-items-center rounded-btn transition-colors disabled:opacity-40 ${
    danger
      ? 'text-muted hover:bg-danger/10 hover:text-danger'
      : 'text-muted hover:bg-hover hover:text-fg'
  }`

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        {href ? (
          <a
            href={href}
            target="_blank"
            rel="noreferrer"
            aria-label={label}
            className={className}
          >
            {children}
          </a>
        ) : (
          <button
            type="button"
            onClick={onClick}
            disabled={disabled}
            aria-label={label}
            className={className}
          >
            {children}
          </button>
        )}
      </TooltipTrigger>
      <TooltipContent>{label}</TooltipContent>
    </Tooltip>
  )
}

function PropertyDialog({
  open,
  onOpenChange,
  property,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  property: Property | null
}) {
  const [priceEur, setPriceEur] = useState(property?.priceEur ?? 0)
  const { data: agents } = useAgents()
  const createProperty = useCreateProperty()
  const updateProperty = useUpdateProperty()

  const busy = createProperty.isPending || updateProperty.isPending

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (busy) return

    const form = new FormData(event.currentTarget)
    const aiAgentId = String(form.get('aiAgentId') ?? '')

    const input: PropertyInput = {
      title: String(form.get('title') ?? '').trim(),
      description: String(form.get('description') ?? '').trim() || null,
      priceEur: Number(form.get('priceEur') ?? 0),
      surfaceSqm: Number(form.get('surfaceSqm') ?? 0),
      rooms: Number(form.get('rooms') ?? 0),
      city: String(form.get('city') ?? '').trim(),
      neighborhood: String(form.get('neighborhood') ?? '').trim() || null,
      propertyType: String(form.get('propertyType') ?? '') as PropertyType,
      listingUrl: String(form.get('listingUrl') ?? '').trim() || null,
      aiAgentId: aiAgentId || null,
    }

    // Dialogul se inchide doar la succes: la eroare rămâi cu datele completate
    try {
      if (property) {
        await updateProperty.mutateAsync({ id: property.id, input })
      } else {
        await createProperty.mutateAsync(input)
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
            {property ? 'Editează proprietatea' : 'Adaugă o proprietate'}
          </DialogTitle>
          <DialogDescription>
            Descrierea ajunge direct în răspunsurile agentului, deci scrie-o ca
            pentru un client.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="flex min-h-0 flex-1 flex-col">
          <DialogBody className="space-y-4">
            <Field label="Titlu" htmlFor="title">
              <Input
                id="title"
                name="title"
                defaultValue={property?.title}
                placeholder="Apartament 2 camere, Gheorgheni"
                required
              />
            </Field>

            <Field label="Descriere" htmlFor="description">
              <Textarea
                id="description"
                name="description"
                rows={4}
                defaultValue={property?.description}
                placeholder="Etaj, an construcție, dotări, ce se vede pe fereastră…"
              />
            </Field>

            <div className="grid gap-4 sm:grid-cols-2">
              <Field
                label="Preț EUR"
                htmlFor="priceEur"
                hint={
                  priceEur > 0 ? (
                    <span className="tnum">
                      ≈ {formatRon(priceEur * EUR_TO_RON)} RON
                    </span>
                  ) : null
                }
              >
                <Input
                  id="priceEur"
                  name="priceEur"
                  type="number"
                  min={0}
                  step={500}
                  value={priceEur || ''}
                  onChange={(event) => setPriceEur(Number(event.target.value))}
                  placeholder="92000"
                  required
                />
              </Field>

              <Field label="Suprafață (m²)" htmlFor="surfaceSqm">
                <Input
                  id="surfaceSqm"
                  name="surfaceSqm"
                  type="number"
                  min={0}
                  defaultValue={property?.surfaceSqm}
                  placeholder="58"
                  required
                />
              </Field>

              <Field label="Camere" htmlFor="rooms">
                <Input
                  id="rooms"
                  name="rooms"
                  type="number"
                  min={0}
                  defaultValue={property?.rooms ?? 0}
                  placeholder="2"
                />
              </Field>

              <Field label="Oraș" htmlFor="city">
                <Input
                  id="city"
                  name="city"
                  defaultValue={property?.city}
                  placeholder="Cluj-Napoca"
                  required
                />
              </Field>

              <Field label="Cartier" htmlFor="neighborhood">
                <Input
                  id="neighborhood"
                  name="neighborhood"
                  defaultValue={property?.neighborhood ?? ''}
                  placeholder="Gheorgheni"
                />
              </Field>

              <Field label="Tip" htmlFor="propertyType">
                <Select
                  id="propertyType"
                  name="propertyType"
                  defaultValue={property?.propertyType ?? 'apartment'}
                >
                  {propertyTypes.map((option) => (
                    <option key={option} value={option}>
                      {propertyTypeLabels[option]}
                    </option>
                  ))}
                </Select>
              </Field>

              <Field label="Agent AI" htmlFor="aiAgentId">
                <Select
                  id="aiAgentId"
                  name="aiAgentId"
                  defaultValue={property?.aiAgentId ?? ''}
                >
                  <option value="">Niciunul</option>
                  {(agents ?? []).map((agent) => (
                    <option key={agent.id} value={agent.id}>
                      {agent.name}
                    </option>
                  ))}
                </Select>
              </Field>

              <Field label="URL listare" htmlFor="listingUrl">
                <Input
                  id="listingUrl"
                  name="listingUrl"
                  type="url"
                  defaultValue={property?.listingUrl ?? ''}
                  placeholder="https://…"
                />
              </Field>
            </div>
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
              {property ? 'Salvează' : 'Adaugă proprietatea'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

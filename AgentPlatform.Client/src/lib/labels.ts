import type {
  AgentLanguage,
  AgentTone,
  ConversationStatus,
  LeadStatus,
  PropertyType,
} from '@/lib/types'

/**
 * Backendul vorbeste in engleza, interfata in romana. Traducerea sta doar aici,
 * ca sa nu apara doua variante ale aceleiasi etichete in doua pagini.
 */

export const propertyTypeLabels: Record<PropertyType, string> = {
  apartment: 'Apartament',
  house: 'Casă',
  land: 'Teren',
  commercial: 'Comercial',
}

export const leadStatusLabels: Record<LeadStatus, string> = {
  new: 'Nou',
  contacted: 'Contactat',
  qualified: 'Calificat',
  lost: 'Pierdut',
}

export const conversationStatusLabels: Record<ConversationStatus, string> = {
  active: 'activă',
  closed: 'închisă',
  converted: 'convertită',
}

export const toneLabels: Record<AgentTone, string> = {
  professional: 'Profesional',
  friendly: 'Prietenos',
  formal: 'Formal',
}

export const languageLabels: Record<AgentLanguage, string> = {
  ro: 'Română',
  en: 'Engleză',
  hu: 'Maghiară',
}

export const propertyTypes = Object.keys(propertyTypeLabels) as PropertyType[]
export const leadStatuses = Object.keys(leadStatusLabels) as LeadStatus[]
export const tones = Object.keys(toneLabels) as AgentTone[]
export const languages = Object.keys(languageLabels) as AgentLanguage[]

// ─── Formatare ──────────────────────────────────────────────────────────────

export const formatEur = (value: number) =>
  value.toLocaleString('ro-RO', { maximumFractionDigits: 0 })

export const formatRon = (value: number) =>
  value.toLocaleString('ro-RO', { maximumFractionDigits: 0 })

export const formatNumber = (value: number) => value.toLocaleString('ro-RO')

/** "26 iul." — compact, pentru coloane de tabel */
export const formatShortDate = (iso: string) =>
  new Date(iso).toLocaleDateString('ro-RO', { day: 'numeric', month: 'short' })

export const formatLongDate = (iso: string) =>
  new Date(iso).toLocaleDateString('ro-RO', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  })

export const formatTime = (iso: string) =>
  new Date(iso).toLocaleTimeString('ro-RO', {
    hour: '2-digit',
    minute: '2-digit',
  })

/** Ziua din saptamana, pentru axa graficului: "Lun", "Mar"… */
export const formatWeekday = (iso: string) => {
  const label = new Date(iso).toLocaleDateString('ro-RO', { weekday: 'short' })
  return label.charAt(0).toUpperCase() + label.slice(1).replace('.', '')
}

/**
 * Ora pentru mesaje: cele de azi arata ora, cele mai vechi arata data.
 * Un timestamp complet pe fiecare bula ar fi zgomot.
 */
export const formatMessageTime = (iso: string) => {
  const date = new Date(iso)
  const isToday = date.toDateString() === new Date().toDateString()
  return isToday ? formatTime(iso) : formatShortDate(iso)
}

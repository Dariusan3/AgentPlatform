import type { BadgeTone } from '@/components/ui/badge'
import type { ConversationStatus, LeadStatus } from '@/lib/types'

/**
 * Maparea status -> culoare sta intr-un singur loc: aceeasi stare trebuie sa
 * arate identic in dashboard, in tabelul de leaduri si in conversatii.
 */
export const conversationTone: Record<ConversationStatus, BadgeTone> = {
  active: 'success',
  closed: 'muted',
  converted: 'amber',
}

export const leadTone: Record<LeadStatus, BadgeTone> = {
  new: 'info',
  contacted: 'outline',
  qualified: 'success',
  lost: 'muted',
}

/** Verde peste 75, amber peste 50, rosu sub. Scorul e o judecata, nu decor. */
export function scoreClass(score: number) {
  if (score >= 75) return 'bg-success'
  if (score >= 50) return 'bg-amber'
  return 'bg-danger'
}

/**
 * Oglindesc DTO-urile din AgentPlatform.Api. Backendul foloseste valori in
 * engleza pentru enum-uri; traducerea in romana se face in lib/labels.ts.
 */

export type PropertyType = 'apartment' | 'house' | 'land' | 'commercial'
export type LeadStatus = 'new' | 'contacted' | 'qualified' | 'lost'
export type ConversationStatus = 'active' | 'closed' | 'converted'
export type AgentTone = 'professional' | 'friendly' | 'formal'
export type AgentLanguage = 'ro' | 'en' | 'hu'
export type MessageRole = 'user' | 'assistant' | 'system'

export type Property = {
  id: string
  tenantId: string
  aiAgentId: string | null
  title: string
  description: string
  priceEur: number
  priceRon: number
  surfaceSqm: number
  rooms: number
  city: string
  neighborhood: string | null
  propertyType: PropertyType
  listingUrl: string | null
  images: string[]
  createdAt: string
}

export type PropertyInput = {
  title: string
  description?: string | null
  priceEur: number
  surfaceSqm: number
  rooms: number
  city: string
  neighborhood?: string | null
  propertyType: PropertyType
  listingUrl?: string | null
  aiAgentId?: string | null
}

export type AiAgent = {
  id: string
  tenantId: string
  name: string
  persona: string
  tone: AgentTone
  language: AgentLanguage
  whatsAppNumber: string | null
  isActive: boolean
  createdAt: string
  conversationsCount: number
  leadsCount: number
}

export type AiAgentInput = {
  name: string
  persona?: string | null
  tone: AgentTone
  language: AgentLanguage
  whatsAppNumber?: string | null
  /** Doar la update; la create backendul porneste agentul activ */
  isActive?: boolean
}

export type Message = {
  id: string
  role: MessageRole
  content: string
  tokensUsed: number | null
  createdAt: string
}

export type Conversation = {
  id: string
  tenantId: string
  aiAgentId: string | null
  contactPhone: string
  contactName: string | null
  channel: string
  status: ConversationStatus
  leadScore: number
  startedAt: string
  lastMessageAt: string | null
  /** In liste contine doar ultimul mesaj; la detaliu, tot istoricul */
  messages: Message[]
  lead: Lead | null
}

export type Lead = {
  id: string
  tenantId: string
  conversationId: string | null
  name: string | null
  phone: string | null
  email: string | null
  budgetMin: number | null
  budgetMax: number | null
  preferredCity: string | null
  preferredType: string | null
  notes: string | null
  status: LeadStatus
  createdAt: string
  conversation: Conversation | null
}

export type WeeklyDataPoint = {
  date: string
  conversations: number
  leads: number
}

export type DashboardStats = {
  activeConversations: number
  newLeads: number
  totalProperties: number
  conversionRate: number
  weeklyData: WeeklyDataPoint[]
  recentConversations: Conversation[]
}

export type Profile = {
  id: string
  email: string
  fullName: string | null
  companyName: string | null
  phone: string | null
  plan: string
  createdAt: string
}

export type ProfileInput = {
  fullName?: string | null
  companyName?: string | null
  phone?: string | null
}

export type Usage = {
  month: string
  messagesCount: number
  tokensUsed: number
  leadsGenerated: number
  /** null = nelimitat */
  messagesLimit: number | null
  leadsLimit: number | null
  plan: string
}

/** Vocile romanesti pe care Azure le are efectiv. Validate identic in API. */
export const voiceNames = ['ro-RO-AlinaNeural', 'ro-RO-EmilNeural'] as const
export type VoiceName = (typeof voiceNames)[number]

export type VoiceAgent = {
  id: string
  name: string
  twilioPhoneNumber: string | null
  voiceName: VoiceName
  systemPrompt: string
  greetingMessage: string
  maxCallDurationSeconds: number
  isActive: boolean
  createdAt: string
  callsCount: number
  qualifiedLeadsCount: number
}

export type VoiceAgentInput = {
  name: string
  twilioPhoneNumber: string | null
  voiceName: VoiceName
  systemPrompt: string | null
  greetingMessage: string | null
  maxCallDurationSeconds: number
  isActive?: boolean
}

export type VoiceMessage = {
  id: string
  role: 'caller' | 'agent'
  content: string
  audioDurationMs: number | null
  createdAt: string
}

export type VoiceCall = {
  id: string
  voiceAgentId: string | null
  callSid: string
  callerPhone: string
  callerName: string | null
  status: string
  durationSeconds: number | null
  transcript: string | null
  leadQualified: boolean
  viewingScheduled: boolean
  viewingDateTime: string | null
  smsSent: boolean
  createdAt: string
  endedAt: string | null
  messages: VoiceMessage[]
}

export type VoiceTestCall = {
  callId: string
  callSid: string
  streamUrl: string
}

export type NotificationSeverity = 'info' | 'success' | 'warning' | 'error'

export type AppNotification = {
  id: string
  type: string
  title: string
  body: string
  severity: NotificationSeverity
  link: string | null
  read: boolean
  createdAt: string
}

export type NotificationList = {
  unreadCount: number
  items: AppNotification[]
}

export type NotificationPreference = {
  type: string
  /** activitate | apeluri | operational */
  group: string
  title: string
  description: string
  inApp: boolean
  push: boolean
}

export type PushConfig = {
  enabled: boolean
  publicKey: string | null
}

/** Cheile de cache, intr-un singur loc, ca invalidarea sa nu rateze nimic. */
export const queryKeys = {
  dashboard: ['dashboard'] as const,
  properties: (filters?: Record<string, string | undefined>) =>
    filters ? (['properties', filters] as const) : (['properties'] as const),
  property: (id: string) => ['properties', id] as const,
  agents: ['agents'] as const,
  agent: (id: string) => ['agents', id] as const,
  leads: (status?: string) => ['leads', status ?? 'all'] as const,
  lead: (id: string) => ['leads', 'detail', id] as const,
  conversations: ['conversations'] as const,
  conversation: (id: string) => ['conversations', id] as const,
  profile: ['settings', 'profile'] as const,
  usage: ['settings', 'usage'] as const,
}

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api, { apiErrorMessage } from '@/lib/api'
import { queryKeys } from '@/lib/queries/keys'
import type { Conversation, Lead } from '@/lib/types'

/** Lista: fiecare conversatie are doar ultimul mesaj, ca previzualizare. */
export function useConversations() {
  return useQuery({
    queryKey: queryKeys.conversations,
    queryFn: async () => {
      const { data } = await api.get<Conversation[]>('/api/conversations')
      return data
    },
  })
}

/** Detaliu: tot istoricul de mesaje plus leadul asociat. */
export function useConversation(id: string | null) {
  return useQuery({
    queryKey: queryKeys.conversation(id ?? ''),
    queryFn: async () => {
      const { data } = await api.get<Conversation>(`/api/conversations/${id}`)
      return data
    },
    enabled: id !== null,
  })
}

export function useCloseConversation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: string) => {
      const { data } = await api.patch<Conversation>(`/api/conversations/${id}/close`)
      return data
    },
    onSuccess: (conversation) => {
      void queryClient.invalidateQueries({ queryKey: ['conversations'] })
      void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard })
      toast.success('Conversația a fost închisă.', {
        description: conversation.contactName ?? conversation.contactPhone,
      })
    },
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

export function useConvertConversation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: string) => {
      const { data } = await api.patch<Lead>(`/api/conversations/${id}/convert`)
      return data
    },
    onSuccess: (lead) => {
      void queryClient.invalidateQueries({ queryKey: ['conversations'] })
      void queryClient.invalidateQueries({ queryKey: ['leads'] })
      void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard })
      toast.success(`${lead.name ?? 'Contactul'} a fost marcat ca lead.`, {
        description: 'Îl găsești în pagina Leaduri.',
      })
    },
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

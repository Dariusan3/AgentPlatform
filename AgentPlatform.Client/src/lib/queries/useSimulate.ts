import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api, { apiErrorMessage } from '@/lib/api'
import { queryKeys } from '@/lib/queries/keys'
import type { Conversation } from '@/lib/types'

export type SimulateInput = {
  aiAgentId: string
  contactPhone: string
  contactName?: string | null
  message: string
}

type SimulateResult = {
  conversation: Conversation
  aiReply: string | null
  replyError: string | null
}

/**
 * Trimite un mesaj ca si cum ar fi venit pe WhatsApp. Endpointul intoarce 200
 * chiar daca generarea raspunsului a eșuat, cu motivul in `replyError` —
 * mesajul primit e salvat oricum.
 */
export function useSimulateMessage() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (input: SimulateInput) => {
      const { data } = await api.post<SimulateResult>(
        '/api/conversations/simulate',
        input,
      )
      return data
    },
    onSuccess: (result) => {
      void queryClient.invalidateQueries({ queryKey: ['conversations'] })
      void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard })
      void queryClient.invalidateQueries({ queryKey: queryKeys.agents })

      if (result.replyError) {
        toast.error('Agentul nu a putut răspunde.', {
          description: result.replyError,
        })
      } else {
        toast.success('Agentul a răspuns.', {
          description: 'Vezi firul complet în conversație.',
        })
      }
    },
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

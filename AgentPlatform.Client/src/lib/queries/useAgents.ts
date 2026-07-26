import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api, { apiErrorMessage } from '@/lib/api'
import { queryKeys } from '@/lib/queries/keys'
import type { AiAgent, AiAgentInput } from '@/lib/types'

export function useAgents() {
  return useQuery({
    queryKey: queryKeys.agents,
    queryFn: async () => {
      const { data } = await api.get<AiAgent[]>('/api/agents')
      return data
    },
  })
}

export function useCreateAgent() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (input: AiAgentInput) => {
      const { data } = await api.post<AiAgent>('/api/agents', input)
      return data
    },
    onSuccess: (agent) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.agents })
      toast.success('Agent creat.', { description: agent.name })
    },
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

export function useUpdateAgent() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, input }: { id: string; input: AiAgentInput }) => {
      const { data } = await api.put<AiAgent>(`/api/agents/${id}`, input)
      return data
    },
    onSuccess: (agent) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.agents })
      toast.success('Agent actualizat.', { description: agent.name })
    },
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

export function useToggleAgent() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (agent: AiAgent) => {
      const { data } = await api.patch<AiAgent>(`/api/agents/${agent.id}/toggle`)
      return data
    },
    onSuccess: (agent) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.agents })
      const shortName = agent.name.split('—')[0].trim()
      toast.success(
        agent.isActive
          ? `${shortName} răspunde din nou.`
          : `${shortName} a fost oprit.`,
        {
          description: agent.isActive
            ? 'Mesajele noi primesc răspuns automat.'
            : 'Mesajele noi rămân necitite până îl repornești.',
        },
      )
    },
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

export function useDeleteAgent() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (agent: AiAgent) => {
      await api.delete(`/api/agents/${agent.id}`)
      return agent
    },
    onSuccess: (agent) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.agents })
      // Proprietatile pot referi agentul sters, deci se reincarca si ele
      void queryClient.invalidateQueries({ queryKey: ['properties'] })
      toast.success('Agent șters.', { description: agent.name })
    },
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

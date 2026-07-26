import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api, { apiErrorMessage } from '@/lib/api'
import { queryKeys } from '@/lib/queries/keys'
import type {
  VoiceAgent,
  VoiceAgentInput,
  VoiceCall,
  VoiceTestCall,
} from '@/lib/types'

export function useVoiceAgents() {
  return useQuery({
    queryKey: queryKeys.voiceAgents,
    queryFn: async () => {
      const { data } = await api.get<VoiceAgent[]>('/api/voice-agents')
      return data
    },
  })
}

export function useVoiceCalls(agentId: string | null) {
  return useQuery({
    queryKey: queryKeys.voiceCalls(agentId ?? ''),
    enabled: Boolean(agentId),
    queryFn: async () => {
      const { data } = await api.get<VoiceCall[]>(
        `/api/voice-agents/${agentId}/calls`,
      )
      return data
    },
  })
}

/**
 * Un apel in desfasurare. Transcrierea se scrie in DB pe masura ce agentul
 * vorbeste, deci o citim ciclic — WebSocketul duce doar audio, ca la Twilio.
 */
export function useVoiceCall(callId: string | null, live: boolean) {
  return useQuery({
    queryKey: queryKeys.voiceCall(callId ?? ''),
    enabled: Boolean(callId),
    refetchInterval: live ? 1500 : false,
    queryFn: async () => {
      const { data } = await api.get<VoiceCall>(`/api/voice-calls/${callId}`)
      return data
    },
  })
}

export function useCreateVoiceAgent() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (input: VoiceAgentInput) => {
      const { data } = await api.post<VoiceAgent>('/api/voice-agents', input)
      return data
    },
    onSuccess: (agent) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.voiceAgents })
      toast.success('Agent vocal creat.', { description: agent.name })
    },
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

export function useUpdateVoiceAgent() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      id,
      input,
    }: {
      id: string
      input: VoiceAgentInput
    }) => {
      const { data } = await api.put<VoiceAgent>(`/api/voice-agents/${id}`, input)
      return data
    },
    onSuccess: (agent) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.voiceAgents })
      toast.success('Agent vocal actualizat.', { description: agent.name })
    },
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

export function useToggleVoiceAgent() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (agent: VoiceAgent) => {
      const { data } = await api.patch<VoiceAgent>(
        `/api/voice-agents/${agent.id}/toggle`,
      )
      return data
    },
    onSuccess: (agent) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.voiceAgents })
      const shortName = agent.name.split('—')[0].trim()
      toast.success(
        agent.isActive
          ? `${shortName} preia din nou apelurile.`
          : `${shortName} a fost oprit.`,
        {
          description: agent.isActive
            ? 'Apelurile primite ajung la agent.'
            : 'Apelurile primite nu mai primesc răspuns.',
        },
      )
    },
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

export function useDeleteVoiceAgent() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (agent: VoiceAgent) => {
      await api.delete(`/api/voice-agents/${agent.id}`)
      return agent
    },
    onSuccess: (agent) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.voiceAgents })
      toast.success('Agent vocal șters.', { description: agent.name })
    },
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

/** Deschide un apel pe care browserul il preia; nu implica Twilio. */
export function useStartTestCall() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (agentId: string) => {
      const { data } = await api.post<VoiceTestCall>(
        `/api/voice-agents/${agentId}/test-call`,
      )
      return data
    },
    onSuccess: (_call, agentId) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.voiceAgents })
      void queryClient.invalidateQueries({
        queryKey: queryKeys.voiceCalls(agentId),
      })
    },
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

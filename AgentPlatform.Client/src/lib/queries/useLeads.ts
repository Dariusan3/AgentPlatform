import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api, { apiErrorMessage } from '@/lib/api'
import { queryKeys } from '@/lib/queries/keys'
import { leadStatusLabels } from '@/lib/labels'
import type { Lead, LeadStatus } from '@/lib/types'

/** Fara status, backendul intoarce toate leadurile. */
export function useLeads(status?: LeadStatus) {
  return useQuery({
    queryKey: queryKeys.leads(status),
    queryFn: async () => {
      const { data } = await api.get<Lead[]>('/api/leads', {
        params: status ? { status } : undefined,
      })
      return data
    },
  })
}

/** Detaliul include conversatia si mesajele ei, pentru drawer. */
export function useLead(id: string | null) {
  return useQuery({
    queryKey: queryKeys.lead(id ?? ''),
    queryFn: async () => {
      const { data } = await api.get<Lead>(`/api/leads/${id}`)
      return data
    },
    enabled: id !== null,
  })
}

export function useUpdateLeadStatus() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, status }: { id: string; status: LeadStatus }) => {
      const { data } = await api.patch<Lead>(`/api/leads/${id}/status`, { status })
      return data
    },
    onSuccess: (lead) => {
      void queryClient.invalidateQueries({ queryKey: ['leads'] })
      void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard })
      toast.success(
        `${lead.name ?? 'Leadul'} e marcat drept ${leadStatusLabels[lead.status].toLowerCase()}.`,
      )
    },
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

export function useUpdateLeadNotes() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, notes }: { id: string; notes: string }) => {
      const { data } = await api.patch<Lead>(`/api/leads/${id}/notes`, { notes })
      return data
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leads'] })
      toast.success('Notițele au fost salvate.')
    },
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

export function useDeleteLead() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (lead: Lead) => {
      await api.delete(`/api/leads/${lead.id}`)
      return lead
    },
    onSuccess: (lead) => {
      void queryClient.invalidateQueries({ queryKey: ['leads'] })
      void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard })
      toast.success('Lead șters.', { description: lead.name ?? undefined })
    },
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api, { apiErrorMessage } from '@/lib/api'
import { queryKeys } from '@/lib/queries/keys'
import type { Profile, ProfileInput, Usage } from '@/lib/types'

export function useProfile() {
  return useQuery({
    queryKey: queryKeys.profile,
    queryFn: async () => {
      const { data } = await api.get<Profile>('/api/settings/profile')
      return data
    },
  })
}

export function useUsage() {
  return useQuery({
    queryKey: queryKeys.usage,
    queryFn: async () => {
      const { data } = await api.get<Usage>('/api/settings/usage')
      return data
    },
  })
}

export function useUpdateProfile() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (input: ProfileInput) => {
      const { data } = await api.put<Profile>('/api/settings/profile', input)
      return data
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['settings'] })
      toast.success('Modificările au fost salvate.')
    },
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

export function useUpdatePassword() {
  return useMutation({
    mutationFn: async (newPassword: string) => {
      await api.put('/api/settings/password', { newPassword })
    },
    onSuccess: () =>
      toast.success('Parola a fost schimbată.', {
        description: 'Folosește-o la următoarea conectare.',
      }),
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

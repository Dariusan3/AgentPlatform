import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api, { apiErrorMessage } from '@/lib/api'
import { queryKeys } from '@/lib/queries/keys'
import type {
  NotificationList,
  NotificationPreference,
  PushConfig,
} from '@/lib/types'

/**
 * Notificarile se reinterogheaza singure la 30 de secunde.
 *
 * Nu e cea mai eleganta solutie, dar e cea corecta aici: evenimentele vin din
 * webhookuri si din apeluri telefonice, deci serverul nu are pe ce canal deschis
 * sa impinga nimic. La 30 de secunde, o notificare apare destul de repede fara
 * sa incarce serverul.
 */
export function useNotifications(limit = 20) {
  return useQuery({
    queryKey: queryKeys.notifications,
    refetchInterval: 30_000,
    refetchOnWindowFocus: true,
    queryFn: async () => {
      const { data } = await api.get<NotificationList>('/api/notifications', {
        params: { limit },
      })
      return data
    },
  })
}

export function useMarkNotificationRead() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: string) => {
      await api.patch(`/api/notifications/${id}/read`)
      return id
    },
    onSuccess: () =>
      void queryClient.invalidateQueries({ queryKey: queryKeys.notifications }),
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

export function useMarkAllNotificationsRead() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async () => {
      const { data } = await api.patch<number>('/api/notifications/read-all')
      return data
    },
    onSuccess: () =>
      void queryClient.invalidateQueries({ queryKey: queryKeys.notifications }),
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

export function useNotificationPreferences() {
  return useQuery({
    queryKey: queryKeys.notificationPreferences,
    queryFn: async () => {
      const { data } = await api.get<NotificationPreference[]>(
        '/api/notifications/preferences',
      )
      return data
    },
  })
}

export function useUpdateNotificationPreference() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (input: {
      type: string
      inApp: boolean
      push: boolean
    }) => {
      const { data } = await api.put<NotificationPreference[]>(
        '/api/notifications/preferences',
        input,
      )
      return data
    },
    // Raspunsul e lista completa: o scriem direct, fara sa mai cerem o data
    onSuccess: (preferences) =>
      queryClient.setQueryData(queryKeys.notificationPreferences, preferences),
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

export function usePushConfig() {
  return useQuery({
    queryKey: queryKeys.pushConfig,
    // Cheia VAPID nu se schimba cat timp sta pagina deschisa
    staleTime: Infinity,
    queryFn: async () => {
      const { data } = await api.get<PushConfig>('/api/notifications/push/config')
      return data
    },
  })
}

export function useSendTestPush() {
  return useMutation({
    mutationFn: async () => {
      await api.post('/api/notifications/push/test')
    },
    onSuccess: () =>
      toast.success('Notificare de probă trimisă.', {
        description: 'Ar trebui să apară în câteva secunde.',
      }),
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

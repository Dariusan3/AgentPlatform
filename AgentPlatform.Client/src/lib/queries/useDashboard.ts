import { useQuery } from '@tanstack/react-query'
import api from '@/lib/api'
import { queryKeys } from '@/lib/queries/keys'
import type { DashboardStats } from '@/lib/types'

export function useDashboardStats() {
  return useQuery({
    queryKey: queryKeys.dashboard,
    queryFn: async () => {
      const { data } = await api.get<DashboardStats>('/api/dashboard/stats')
      return data
    },
  })
}

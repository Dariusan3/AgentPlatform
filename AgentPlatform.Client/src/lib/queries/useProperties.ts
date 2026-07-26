import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api, { apiErrorMessage } from '@/lib/api'
import { queryKeys } from '@/lib/queries/keys'
import type { Property, PropertyInput } from '@/lib/types'

export type PropertyFilters = {
  search?: string
  type?: string
  city?: string
}

export function useProperties(filters: PropertyFilters) {
  // Trimitem doar filtrele completate: `?type=` gol ar fi tratat ca valoare
  const params = Object.fromEntries(
    Object.entries(filters).filter(([, value]) => Boolean(value)),
  )

  return useQuery({
    queryKey: queryKeys.properties(params),
    queryFn: async () => {
      const { data } = await api.get<Property[]>('/api/properties', { params })
      return data
    },
  })
}

export function useCreateProperty() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (input: PropertyInput) => {
      const { data } = await api.post<Property>('/api/properties', input)
      return data
    },
    onSuccess: (property) => {
      // Dashboardul numara proprietatile, deci se invalideaza si el
      void queryClient.invalidateQueries({ queryKey: ['properties'] })
      void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard })
      toast.success('Proprietate adăugată.', { description: property.title })
    },
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

export function useUpdateProperty() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, input }: { id: string; input: PropertyInput }) => {
      const { data } = await api.put<Property>(`/api/properties/${id}`, input)
      return data
    },
    onSuccess: (property) => {
      void queryClient.invalidateQueries({ queryKey: ['properties'] })
      toast.success('Modificările au fost salvate.', {
        description: property.title,
      })
    },
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

export function useDeleteProperty() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (property: Property) => {
      await api.delete(`/api/properties/${property.id}`)
      return property
    },
    onSuccess: (property) => {
      void queryClient.invalidateQueries({ queryKey: ['properties'] })
      void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard })
      toast.success('Proprietate ștearsă.', { description: property.title })
    },
    onError: (error) => toast.error(apiErrorMessage(error)),
  })
}

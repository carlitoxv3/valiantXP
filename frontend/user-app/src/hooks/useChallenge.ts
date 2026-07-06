import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { DynamicChallenge } from '@/types/api'

export const useChallenge = (id: string) =>
  useQuery<DynamicChallenge>({
    queryKey: ['challenge', id],
    queryFn: () => api.get(`/dynamics/${id}`).then((r) => r.data),
    enabled: !!id,
  })

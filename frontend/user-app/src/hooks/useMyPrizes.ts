import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { UserPrize } from '@/types/api'

export const useMyPrizes = () =>
  useQuery<UserPrize[]>({
    queryKey: ['prizes'],
    queryFn: () => api.get('/users/prizes').then((r) => r.data),
  })

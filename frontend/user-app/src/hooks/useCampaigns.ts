import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { Campaign } from '@/types/api'

export const useCampaigns = () =>
  useQuery<Campaign[]>({
    queryKey: ['campaigns'],
    queryFn: () => api.get<Campaign[]>('/campaigns').then((r) => r.data),
    staleTime: 1000 * 60 * 5, // 5 min
  })

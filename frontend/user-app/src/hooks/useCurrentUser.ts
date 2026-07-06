import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { UserProfile } from '@/types/api'

export const useCurrentUser = () =>
  useQuery<UserProfile>({
    queryKey: ['me'],
    queryFn: () => api.get('/users/me').then((r) => r.data),
    enabled: !!localStorage.getItem('vxp_access_token'),
  })

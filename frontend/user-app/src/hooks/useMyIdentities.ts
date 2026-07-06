import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { UserIdentity } from '@/types/api'

export const useMyIdentities = () =>
  useQuery<UserIdentity[]>({
    queryKey: ['identities'],
    queryFn: () => api.get('/users/me/identities').then((r) => r.data),
  })

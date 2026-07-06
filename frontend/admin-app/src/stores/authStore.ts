import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { UserProfile } from '@/types/api'

interface AuthState {
  token: string | null
  user: UserProfile | null
  setAuth: (token: string, user: UserProfile) => void
  clearAuth: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      user: null,
      setAuth: (token, user) => {
        localStorage.setItem('vxp_admin_token', token)
        set({ token, user })
      },
      clearAuth: () => {
        localStorage.removeItem('vxp_admin_token')
        set({ token: null, user: null })
      },
    }),
    { name: 'vxp-admin-auth' }
  )
)

import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { UserProfile } from '@/types/api'

interface AuthState {
  token: string | null
  user: UserProfile | null
  guestToken: string | null
  isAuthenticated: boolean
  setAuth: (token: string, user: UserProfile) => void
  setGuestToken: (token: string) => void
  clearAuth: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      user: null,
      guestToken: null,
      isAuthenticated: false,
      setAuth: (token, user) => {
        localStorage.setItem('vxp_access_token', token)
        set({ token, user, isAuthenticated: true, guestToken: null })
      },
      setGuestToken: (token) => set({ guestToken: token }),
      clearAuth: () => {
        localStorage.removeItem('vxp_access_token')
        localStorage.removeItem('vxp_user')
        set({ token: null, user: null, isAuthenticated: false })
      },
    }),
    { name: 'vxp-auth', partialize: (s) => ({ token: s.token, user: s.user }) }
  )
)

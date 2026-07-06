import { describe, it, expect, beforeEach } from 'vitest'
import { act } from '@testing-library/react'
import { useAuthStore } from '@/stores/authStore'

const mockUser = {
  id: 'u1',
  email: 'test@test.com',
  totalPoints: 100,
  createdAt: '2024-01-01',
}

beforeEach(() => {
  // Reset store between tests
  useAuthStore.setState({
    token: null,
    user: null,
    guestToken: null,
    isAuthenticated: false,
  })
})

describe('authStore', () => {
  it('starts unauthenticated', () => {
    const { isAuthenticated, token, user } = useAuthStore.getState()
    expect(isAuthenticated).toBe(false)
    expect(token).toBeNull()
    expect(user).toBeNull()
  })

  it('setAuth sets token and user', () => {
    act(() => useAuthStore.getState().setAuth('my-token', mockUser))
    const state = useAuthStore.getState()
    expect(state.token).toBe('my-token')
    expect(state.user?.email).toBe('test@test.com')
    expect(state.isAuthenticated).toBe(true)
  })

  it('clearAuth resets everything', () => {
    act(() => {
      useAuthStore.getState().setAuth('token', mockUser)
      useAuthStore.getState().clearAuth()
    })
    const state = useAuthStore.getState()
    expect(state.token).toBeNull()
    expect(state.isAuthenticated).toBe(false)
  })

  it('setGuestToken stores guest token', () => {
    act(() => useAuthStore.getState().setGuestToken('guest-abc'))
    expect(useAuthStore.getState().guestToken).toBe('guest-abc')
  })

  it('setAuth clears guestToken', () => {
    act(() => {
      useAuthStore.getState().setGuestToken('guest-abc')
      useAuthStore.getState().setAuth('token', mockUser)
    })
    expect(useAuthStore.getState().guestToken).toBeNull()
  })
})

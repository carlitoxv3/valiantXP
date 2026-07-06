import axios from 'axios'

const BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000'

export const api = axios.create({
  baseURL: `${BASE_URL}/api`,
  headers: { 'Content-Type': 'application/json' },
})

// JWT interceptor
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('vxp_access_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// 401 → clear auth
api.interceptors.response.use(
  (res) => res,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('vxp_access_token')
      localStorage.removeItem('vxp_user')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

import { useEffect, useState } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { motion } from 'framer-motion'
import { api } from '@/lib/api'
import { useAuthStore } from '@/stores/authStore'
import type { UserProfile } from '@/types/api'

interface CallbackResponse {
  status: 'resolved' | 'linking_required'
  tokens?: { accessToken: string; refreshToken: string; user: UserProfile }
  pendingLinkToken?: string
}

export default function OAuthCallbackPage() {
  const { provider } = useParams<{ provider: string }>()
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const setAuth = useAuthStore((s) => s.setAuth)
  const [linking, setLinking] = useState(false)
  const [pendingToken, setPendingToken] = useState<string | null>(null)
  const [error, setError] = useState('')
  const [confirmLoading, setConfirmLoading] = useState(false)

  useEffect(() => {
    const code = searchParams.get('code')
    const state = searchParams.get('state')
    if (!provider || !code) {
      navigate('/login')
      return
    }

    const doCallback = async () => {
      try {
        const res = await api.get<CallbackResponse>(
          `/auth/oauth/${provider}/callback?code=${code}&state=${state ?? ''}`
        )
        if (res.data.status === 'resolved' && res.data.tokens) {
          setAuth(res.data.tokens.accessToken, res.data.tokens.user)
          navigate('/')
        } else if (res.data.status === 'linking_required' && res.data.pendingLinkToken) {
          setPendingToken(res.data.pendingLinkToken)
          setLinking(true)
        }
      } catch {
        setError('Error al autenticar. Por favor intenta de nuevo.')
      }
    }

    doCallback()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const handleConfirmLink = async () => {
    if (!pendingToken) return
    setConfirmLoading(true)
    try {
      await api.post('/auth/oauth/link/confirm', { pendingLinkToken: pendingToken })
      navigate('/login')
    } catch {
      setError('Error al vincular la cuenta.')
    } finally {
      setConfirmLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-zinc-950 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-gradient-to-br from-violet-900/20 via-zinc-950 to-blue-900/20 pointer-events-none" />

      <motion.div
        initial={{ opacity: 0, scale: 0.9 }}
        animate={{ opacity: 1, scale: 1 }}
        className="relative bg-white/5 backdrop-blur-xl border border-white/10 rounded-3xl p-8 max-w-md w-full text-center shadow-2xl"
      >
        {error ? (
          <>
            <div className="text-5xl mb-4">❌</div>
            <h2 className="text-xl font-bold text-white mb-2">Error de autenticación</h2>
            <p className="text-zinc-400 text-sm mb-6">{error}</p>
            <button
              onClick={() => navigate('/login')}
              className="text-violet-400 hover:text-violet-300 underline text-sm"
            >
              ← Volver al login
            </button>
          </>
        ) : linking ? (
          <>
            <div className="text-5xl mb-4">🔗</div>
            <h2 className="text-xl font-bold text-white mb-2">Vincular cuenta</h2>
            <p className="text-zinc-400 text-sm mb-6">
              Esta cuenta de {provider} ya está asociada a un usuario diferente.
              ¿Deseas vincularla a tu cuenta actual?
            </p>
            <div className="flex gap-3">
              <button
                onClick={() => navigate('/login')}
                className="flex-1 py-2.5 rounded-xl border border-white/10 text-zinc-400 hover:bg-white/5 text-sm"
              >
                Cancelar
              </button>
              <button
                onClick={handleConfirmLink}
                disabled={confirmLoading}
                className="flex-1 py-2.5 rounded-xl bg-violet-600 hover:bg-violet-500 text-white font-semibold text-sm"
              >
                {confirmLoading ? 'Vinculando...' : 'Confirmar'}
              </button>
            </div>
          </>
        ) : (
          <>
            <div className="w-14 h-14 border-2 border-violet-500 border-t-transparent rounded-full animate-spin mx-auto mb-6" />
            <h2 className="text-xl font-bold text-white mb-2">Autenticando...</h2>
            <p className="text-zinc-500 text-sm">Completando inicio de sesión con {provider}</p>
          </>
        )}
      </motion.div>
    </div>
  )
}

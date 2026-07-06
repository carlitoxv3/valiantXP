import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { motion, AnimatePresence } from 'framer-motion'
import { Mail, ArrowLeft, RefreshCw } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { OtpInput } from '@/components/auth/OtpInput'
import { SocialButton } from '@/components/auth/SocialButton'
import { api } from '@/lib/api'
import { useAuthStore } from '@/stores/authStore'
import type { UserProfile } from '@/types/api'

type Step = 'method' | 'otp-sent' | 'social-linking'

const GOOGLE_ENABLED = import.meta.env.VITE_OAUTH_GOOGLE_ENABLED !== 'false'
const SPOTIFY_ENABLED = import.meta.env.VITE_OAUTH_SPOTIFY_ENABLED === 'true'
const TWITCH_ENABLED = import.meta.env.VITE_OAUTH_TWITCH_ENABLED === 'true'

function maskEmail(email: string) {
  const [user, domain] = email.split('@')
  if (!user || !domain) return email
  const visible = user.slice(0, 2)
  return `${visible}${'*'.repeat(Math.max(0, user.length - 2))}@${domain}`
}

export default function LoginPage() {
  const navigate = useNavigate()
  const setAuth = useAuthStore((s) => s.setAuth)
  const setGuestToken = useAuthStore((s) => s.setGuestToken)

  const [step, setStep] = useState<Step>('method')
  const [email, setEmail] = useState('')
  const [otpValue, setOtpValue] = useState<string[]>(Array(6).fill(''))
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [cooldown, setCooldown] = useState(0)

  // Cooldown timer
  useEffect(() => {
    if (cooldown <= 0) return
    const t = setTimeout(() => setCooldown((c) => c - 1), 1000)
    return () => clearTimeout(t)
  }, [cooldown])

  const handleSendOtp = async (e?: React.FormEvent) => {
    e?.preventDefault()
    if (!email.trim()) return
    setLoading(true)
    setError('')
    try {
      // Backend expects: { target, channel } NOT { email, channel }
      await api.post('/auth/otp/request', { target: email.trim(), channel: 'Email' })
      setStep('otp-sent')
      setCooldown(60)
      setOtpValue(Array(6).fill(''))
    } catch {
      setError('No pudimos enviar el código. Intenta de nuevo.')
    } finally {
      setLoading(false)
    }
  }

  const handleVerifyOtp = async (digits: string[]) => {
    const code = digits.join('')
    if (code.length < 6) return
    setLoading(true)
    setError('')
    try {
      // Backend returns: { isMfaRequired, tokens: { accessToken, refreshToken, expiresAt } }
      const res = await api.post<{ isMfaRequired: boolean; tokens: { accessToken: string } }>(
        '/auth/otp/verify',
        { target: email, code }
      )
      if (res.data.isMfaRequired) {
        setError('MFA no soportado en esta demo.')
        setLoading(false)
        return
      }
      const accessToken = res.data.tokens.accessToken
      // Fetch user profile with the new token
      const profileRes = await api.get<UserProfile>('/users/me', {
        headers: { Authorization: `Bearer ${accessToken}` },
      })
      setAuth(accessToken, profileRes.data)
      navigate('/')
    } catch {
      setError('Código inválido o expirado.')
      setLoading(false)
    }
  }

  // Auto-verify when all 6 digits filled
  useEffect(() => {
    if (step === 'otp-sent' && otpValue.every(Boolean)) {
      handleVerifyOtp(otpValue)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [otpValue, step])

  const handleSocialLogin = async (provider: string) => {
    try {
      // Redirect directly to backend OAuth endpoint
      window.location.href = `${import.meta.env.VITE_API_URL}/api/auth/oauth/${provider.toLowerCase()}`
    } catch {
      setError('No se pudo iniciar el login social.')
    }
  }

  const handleGuest = async () => {
    try {
      const res = await api.post<{ token: string }>('/guest/session')
      setGuestToken(res.data.token)
      navigate('/')
    } catch {
      navigate('/')
    }
  }

  return (
    <div className="min-h-screen bg-zinc-950 flex items-center justify-center p-4 relative overflow-hidden">
      {/* Background gradient */}
      <div className="absolute inset-0 bg-gradient-to-br from-violet-900/20 via-zinc-950 to-blue-900/20 pointer-events-none" />
      <div className="absolute top-1/4 left-1/4 w-96 h-96 bg-violet-600/10 rounded-full blur-3xl pointer-events-none" />
      <div className="absolute bottom-1/4 right-1/4 w-80 h-80 bg-blue-600/10 rounded-full blur-3xl pointer-events-none" />

      <motion.div
        initial={{ opacity: 0, y: 24, scale: 0.97 }}
        animate={{ opacity: 1, y: 0, scale: 1 }}
        transition={{ duration: 0.4, ease: 'easeOut' }}
        className="relative w-full max-w-md"
      >
        {/* Card */}
        <div className="bg-white/5 backdrop-blur-xl border border-white/10 rounded-3xl p-8 shadow-2xl shadow-black/40">
          {/* Logo */}
          <div className="text-center mb-8">
            <div className="w-12 h-12 rounded-2xl bg-gradient-to-br from-violet-500 to-blue-500 flex items-center justify-center mx-auto mb-4 shadow-lg shadow-violet-500/30">
              <span className="text-white font-black text-xl">V</span>
            </div>
            <h1 className="text-2xl font-bold text-white tracking-tight">
              Valiant<span className="text-violet-400">XP</span>
            </h1>
          </div>

          <AnimatePresence mode="wait">
            {step === 'method' && (
              <motion.div
                key="method"
                initial={{ opacity: 0, x: 20 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -20 }}
                className="space-y-5"
              >
                <div className="text-center mb-6">
                  <h2 className="text-lg font-semibold text-zinc-200">Bienvenido</h2>
                  <p className="text-sm text-zinc-500 mt-1">Inicia sesión para continuar</p>
                </div>

                {/* Email form */}
                <form onSubmit={handleSendOtp} className="space-y-3">
                  <div className="relative">
                    <Mail className="absolute left-4 top-1/2 -translate-y-1/2 w-4 h-4 text-zinc-500" />
                    <input
                      type="email"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      placeholder="tu@email.com"
                      required
                      className="w-full pl-11 pr-4 py-3.5 rounded-xl bg-white/5 border border-white/10 text-white placeholder-zinc-600 focus:border-violet-500/60 focus:outline-none focus:bg-violet-500/5 focus:shadow-lg focus:shadow-violet-500/10 transition-all text-sm"
                    />
                  </div>
                  <Button
                    type="submit"
                    disabled={loading || !email}
                    className="w-full bg-violet-600 hover:bg-violet-500 shadow-lg shadow-violet-500/25 py-6 font-semibold"
                  >
                    {loading ? 'Enviando...' : 'Enviar código →'}
                  </Button>
                </form>

                {/* Divider */}
                {(GOOGLE_ENABLED || SPOTIFY_ENABLED || TWITCH_ENABLED) && (
                  <>
                    <div className="relative flex items-center gap-4 my-2">
                      <div className="flex-1 h-px bg-white/10" />
                      <span className="text-xs text-zinc-600 font-medium">o continúa con</span>
                      <div className="flex-1 h-px bg-white/10" />
                    </div>

                    <div className="space-y-2">
                      {GOOGLE_ENABLED && (
                        <SocialButton provider="google" onClick={() => handleSocialLogin('Google')} />
                      )}
                      {SPOTIFY_ENABLED && (
                        <SocialButton provider="spotify" onClick={() => handleSocialLogin('Spotify')} />
                      )}
                      {TWITCH_ENABLED && (
                        <SocialButton provider="twitch" onClick={() => handleSocialLogin('Twitch')} />
                      )}
                    </div>
                  </>
                )}

                {error && <p className="text-red-400 text-sm text-center">{error}</p>}

                {/* Guest link */}
                <div className="text-center pt-2">
                  <button
                    onClick={handleGuest}
                    className="text-xs text-zinc-600 hover:text-zinc-400 transition-colors underline underline-offset-2"
                  >
                    Continuar como invitado →
                  </button>
                </div>
              </motion.div>
            )}

            {step === 'otp-sent' && (
              <motion.div
                key="otp-sent"
                initial={{ opacity: 0, x: 20 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -20 }}
                className="space-y-6"
              >
                <button
                  onClick={() => setStep('method')}
                  className="flex items-center gap-2 text-sm text-zinc-500 hover:text-zinc-300 transition-colors mb-2"
                >
                  <ArrowLeft className="w-4 h-4" />
                  Volver
                </button>

                <div className="text-center">
                  <div className="w-14 h-14 rounded-2xl bg-violet-500/10 border border-violet-500/20 flex items-center justify-center mx-auto mb-4">
                    <Mail className="w-7 h-7 text-violet-400" />
                  </div>
                  <h2 className="text-lg font-semibold text-zinc-200">Revisa tu email</h2>
                  <p className="text-sm text-zinc-500 mt-2">
                    Enviamos un código a <span className="text-violet-400 font-medium">{maskEmail(email)}</span>
                  </p>
                </div>

                <OtpInput value={otpValue} onChange={setOtpValue} disabled={loading} />

                {loading && (
                  <p className="text-center text-violet-400 text-sm animate-pulse">Verificando...</p>
                )}

                {error && <p className="text-red-400 text-sm text-center">{error}</p>}

                <div className="text-center">
                  <button
                    disabled={cooldown > 0 || loading}
                    onClick={() => handleSendOtp()}
                    className="flex items-center gap-2 text-sm text-zinc-500 hover:text-zinc-300 transition-colors mx-auto disabled:opacity-40 disabled:cursor-not-allowed"
                  >
                    <RefreshCw className="w-3.5 h-3.5" />
                    {cooldown > 0 ? `Reenviar en ${cooldown}s` : 'Reenviar código'}
                  </button>
                </div>
              </motion.div>
            )}
          </AnimatePresence>
        </div>

        <p className="text-center text-xs text-zinc-700 mt-6">
          Al continuar, aceptas nuestros términos de servicio
        </p>
      </motion.div>
    </div>
  )
}

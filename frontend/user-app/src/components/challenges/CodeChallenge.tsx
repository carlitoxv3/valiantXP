import { useState, useEffect } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { Loader2, RefreshCw } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import type { ChallengeResult } from '@/types/api'

type CodeState = 'input' | 'loading' | 'winner' | 'loser'

const TIER_CONFIG = {
  gold:   { label: 'Oro',    color: 'from-yellow-500 to-amber-600',  emoji: '🥇' },
  silver: { label: 'Plata',  color: 'from-slate-400 to-slate-500',   emoji: '🥈' },
  bronze: { label: 'Bronce', color: 'from-orange-600 to-orange-700', emoji: '🥉' },
} as const

interface CodeChallengeProps {
  onSubmit: (code: string) => Promise<ChallengeResult>
}

export function CodeChallenge({ onSubmit }: CodeChallengeProps) {
  const [code, setCode] = useState('')
  const [state, setState] = useState<CodeState>('input')
  const [result, setResult] = useState<ChallengeResult | null>(null)
  const [displayNumber, setDisplayNumber] = useState(0)
  const [countdown, setCountdown] = useState(3)

  // Derived from payload
  const position =
    typeof result?.payload?.Position === 'number' ? result.payload.Position : null
  const prizeTier =
    typeof result?.payload?.PrizeTier === 'string'
      ? (result.payload.PrizeTier as keyof typeof TIER_CONFIG)
      : null
  const tierConfig = prizeTier && prizeTier in TIER_CONFIG ? TIER_CONFIG[prizeTier] : null

  const handleSubmit = async () => {
    if (!code.trim()) return
    setState('loading')

    // Spinning number animation while waiting for response
    const interval = setInterval(() => {
      setDisplayNumber(Math.floor(Math.random() * 999) + 1)
    }, 80)

    try {
      const res = await onSubmit(code.trim().toUpperCase())
      setResult(res)
      clearInterval(interval)

      // Settle on the final position number
      const finalPos =
        typeof res.payload?.Position === 'number' ? res.payload.Position : 0
      setDisplayNumber(finalPos)

      // Short pause before revealing result state
      setTimeout(() => {
        setState(res.payload?.IsWinner === true ? 'winner' : 'loser')
      }, 600)
    } catch {
      clearInterval(interval)
      setState('input')
    }
  }

  // Countdown tick — only runs while in 'winner' state
  useEffect(() => {
    if (state !== 'winner') return
    if (countdown <= 0) return
    const t = setTimeout(() => setCountdown((c) => c - 1), 1000)
    return () => clearTimeout(t)
  }, [state, countdown])

  const reset = () => {
    setCode('')
    setState('input')
    setResult(null)
    setCountdown(3)
    setDisplayNumber(0)
  }

  return (
    <div className="space-y-6">
      <AnimatePresence mode="wait">

        {/* ───────── INPUT ───────── */}
        {state === 'input' && (
          <motion.div
            key="input"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -20 }}
            className="space-y-6"
          >
            <div className="text-center">
              <div className="text-5xl mb-4">🎟️</div>
              <h3 className="text-xl font-bold text-white">Ingresa tu código</h3>
              <p className="text-zinc-400 text-sm mt-1">Descubre tu posición del día</p>
            </div>

            <div className="flex gap-3">
              <Input
                value={code}
                onChange={(e) => setCode(e.target.value.toUpperCase())}
                onKeyDown={(e) => e.key === 'Enter' && handleSubmit()}
                placeholder="Ej: MESA001"
                className="flex-1 bg-white/5 border-white/20 text-white placeholder:text-zinc-500 text-center text-lg font-mono tracking-widest uppercase"
              />
              <Button
                onClick={handleSubmit}
                disabled={!code.trim()}
                className="bg-violet-600 hover:bg-violet-500 px-6"
              >
                Validar
              </Button>
            </div>
          </motion.div>
        )}

        {/* ───────── LOADING ───────── */}
        {state === 'loading' && (
          <motion.div
            key="loading"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="text-center space-y-6 py-8"
          >
            <Loader2 className="w-10 h-10 text-violet-400 animate-spin mx-auto" />
            <p className="text-zinc-400">Calculando tu posición...</p>
            <motion.div
              className="text-7xl font-black text-white font-mono"
              animate={{ scale: [1, 1.05, 1] }}
              transition={{ repeat: Infinity, duration: 0.16 }}
            >
              #{displayNumber}
            </motion.div>
          </motion.div>
        )}

        {/* ───────── WINNER ───────── */}
        {state === 'winner' && (
          <motion.div
            key="winner"
            initial={{ opacity: 0, scale: 0.8 }}
            animate={{ opacity: 1, scale: 1 }}
            className="text-center space-y-6 py-4"
          >
            <motion.div
              animate={{ rotate: [-5, 5, -5, 5, 0], scale: [1, 1.2, 1] }}
              transition={{ duration: 0.6 }}
              className="text-6xl"
            >
              🏆
            </motion.div>

            <div>
              <h2 className="text-2xl font-black text-white">¡Posición Ganadora!</h2>
              <p className="text-zinc-400 mt-1">
                Visitante{' '}
                <span className="text-white font-bold text-xl">#{position}</span> del día
              </p>
            </div>

            {tierConfig && (
              <div
                className={`inline-flex items-center gap-2 px-6 py-3 rounded-full bg-gradient-to-r ${tierConfig.color} text-white font-bold text-lg shadow-lg`}
              >
                {tierConfig.emoji} Premio {tierConfig.label}
              </div>
            )}

            <div className="space-y-2">
              <p className="text-zinc-300 text-sm">¡Demuestra tu conocimiento para ganar!</p>
              <div className="flex items-center justify-center gap-3">
                <Loader2 className="w-4 h-4 text-violet-400 animate-spin" />
                <span className="text-violet-400 font-mono font-bold">
                  Cargando trivia en {countdown}...
                </span>
              </div>
              <div className="w-full bg-white/10 rounded-full h-2 overflow-hidden">
                <motion.div
                  className="h-full bg-violet-500"
                  initial={{ width: '0%' }}
                  animate={{
                    width: countdown <= 0 ? '100%' : `${((3 - countdown) / 3) * 100}%`,
                  }}
                  transition={{ duration: 1 }}
                />
              </div>
            </div>
          </motion.div>
        )}

        {/* ───────── LOSER ───────── */}
        {state === 'loser' && (
          <motion.div
            key="loser"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            className="text-center space-y-6 py-8"
          >
            <div className="text-6xl">😔</div>

            <div>
              <h2 className="text-2xl font-bold text-white">¡Gracias por participar!</h2>
              <p className="text-zinc-400 mt-2">
                Tu posición de hoy:{' '}
                <span className="text-white font-bold">#{position}</span>
              </p>
            </div>

            <div className="max-w-xs mx-auto p-4 rounded-xl bg-white/5 border border-white/10">
              <p className="text-zinc-400 text-sm">
                Las posiciones ganadoras son especiales.
              </p>
              <p className="text-zinc-300 text-sm font-medium mt-1">
                ¡Intenta mañana con un nuevo código!
              </p>
            </div>

            <Button
              onClick={reset}
              variant="outline"
              className="gap-2 border-white/20 text-zinc-300 hover:bg-white/5"
            >
              <RefreshCw className="w-4 h-4" />
              Intentar con otro código
            </Button>
          </motion.div>
        )}

      </AnimatePresence>

    </div>
  )
}

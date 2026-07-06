import { useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { Button } from '@/components/ui/button'
import { CheckCircle, XCircle, Code2 } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { ChallengeResult } from '@/types/api'

interface CodeChallengeProps {
  onSubmit?: (code: string) => Promise<ChallengeResult>
  demoMode?: boolean
  demoValidCode?: string
}

type FeedbackState = 'idle' | 'loading' | 'success' | 'error' | 'used'

export function CodeChallenge({ onSubmit, demoMode = false, demoValidCode = 'DEMO2024' }: CodeChallengeProps) {
  const [code, setCode] = useState('')
  const [feedback, setFeedback] = useState<FeedbackState>('idle')
  const [result, setResult] = useState<ChallengeResult | null>(null)
  const [shakeKey, setShakeKey] = useState(0)

  const handleSubmit = async () => {
    if (!code.trim()) return
    setFeedback('loading')

    if (demoMode) {
      await new Promise((r) => setTimeout(r, 800))
      const normalized = code.trim().toUpperCase()
      if (normalized === demoValidCode.toUpperCase()) {
        setFeedback('success')
        setResult({ success: true, message: '¡Código válido! Premio asignado.', pointsAwarded: 500 })
      } else {
        setFeedback('error')
        setShakeKey((k) => k + 1)
        setResult({ success: false, message: 'Código inválido. Intenta con: DEMO2024' })
      }
      return
    }

    if (onSubmit) {
      try {
        const res = await onSubmit(code.trim())
        if (res.success) {
          setFeedback('success')
        } else {
          setFeedback(res.message?.toLowerCase().includes('utilizado') ? 'used' : 'error')
          setShakeKey((k) => k + 1)
        }
        setResult(res)
      } catch {
        setFeedback('error')
        setShakeKey((k) => k + 1)
      }
    }
  }

  const reset = () => {
    setCode('')
    setFeedback('idle')
    setResult(null)
  }

  return (
    <div className="space-y-6 max-w-md mx-auto">
      {/* Icon + title */}
      <div className="text-center">
        <div className="w-16 h-16 rounded-2xl bg-amber-500/10 border border-amber-500/20 flex items-center justify-center mx-auto mb-4">
          <Code2 className="w-8 h-8 text-amber-400" />
        </div>
        <h3 className="text-xl font-bold text-white">Ingresa tu código</h3>
        <p className="text-sm text-zinc-500 mt-1">Introduce el código único que recibiste</p>
      </div>

      {/* Input */}
      <motion.div key={shakeKey} animate={feedback === 'error' || feedback === 'used' ? {
        x: [0, -10, 10, -10, 10, -6, 6, 0],
        transition: { duration: 0.5 }
      } : {}}>
        <input
          type="text"
          value={code}
          onChange={(e) => setCode(e.target.value.toUpperCase())}
          onKeyDown={(e) => e.key === 'Enter' && feedback === 'idle' && handleSubmit()}
          placeholder="XXXX-XXXX"
          disabled={feedback === 'loading' || feedback === 'success'}
          className={cn(
            'w-full text-center text-2xl font-mono font-bold tracking-[0.4em] px-6 py-5 rounded-2xl border-2',
            'bg-white/5 text-white placeholder-zinc-700 outline-none transition-all',
            feedback === 'idle' && 'border-white/10 focus:border-violet-500/60 focus:bg-violet-500/5 focus:shadow-lg focus:shadow-violet-500/10',
            feedback === 'loading' && 'border-violet-500/40 animate-pulse',
            feedback === 'success' && 'border-emerald-500/60 bg-emerald-500/10 shadow-lg shadow-emerald-500/10',
            (feedback === 'error' || feedback === 'used') && 'border-red-500/60 bg-red-500/10',
          )}
        />
      </motion.div>

      {/* Feedback message */}
      <AnimatePresence>
        {result && (
          <motion.div
            initial={{ opacity: 0, y: -10 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0 }}
            className={cn(
              'flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-medium',
              feedback === 'success' && 'bg-emerald-500/10 border border-emerald-500/20 text-emerald-300',
              (feedback === 'error' || feedback === 'used') && 'bg-red-500/10 border border-red-500/20 text-red-300',
            )}
          >
            {feedback === 'success' ? (
              <CheckCircle className="w-5 h-5 flex-shrink-0" />
            ) : (
              <XCircle className="w-5 h-5 flex-shrink-0" />
            )}
            <div>
              <p className="font-semibold">{result.success ? '¡Código válido!' : feedback === 'used' ? 'Código ya utilizado' : 'Código inválido'}</p>
              <p className="opacity-80 text-xs mt-0.5">{result.message}</p>
              {result.pointsAwarded && result.pointsAwarded > 0 && (
                <p className="text-violet-300 font-bold text-lg">+{result.pointsAwarded} puntos</p>
              )}
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Button */}
      {feedback === 'success' ? (
        <Button onClick={reset} variant="outline" className="w-full border-white/10 text-zinc-300 hover:bg-white/5">
          Ingresar otro código
        </Button>
      ) : (
        <Button
          onClick={handleSubmit}
          disabled={!code.trim() || feedback === 'loading'}
          className="w-full bg-amber-500 hover:bg-amber-400 text-zinc-900 font-bold transition-all shadow-lg shadow-amber-500/20"
        >
          {feedback === 'loading' ? (
            <span className="flex items-center gap-2">
              <span className="w-4 h-4 border-2 border-zinc-900/30 border-t-zinc-900 rounded-full animate-spin" />
              Validando...
            </span>
          ) : 'Validar código'}
        </Button>
      )}
    </div>
  )
}

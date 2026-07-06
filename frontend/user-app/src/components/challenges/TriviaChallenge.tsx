import { useState, useEffect, useCallback } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { Progress } from '@/components/ui/progress'
import { Button } from '@/components/ui/button'
import { Trophy, Star, ChevronRight, RotateCcw } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { TriviaConfig, TriviaQuestion, ChallengeResult } from '@/types/api'

interface TriviaChallengeProps {
  config: TriviaConfig
  onSubmit?: (answers: number[]) => Promise<ChallengeResult>
  demoMode?: boolean
}

type GameState = 'playing' | 'result' | 'prize'

function ConfettiPiece({ index }: { index: number }) {
  const colors = ['#8b5cf6', '#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#ec4899']
  const color = colors[index % colors.length]
  const x = (Math.random() - 0.5) * 400
  const delay = Math.random() * 0.5
  return (
    <motion.div
      className="absolute w-2 h-2 rounded-sm top-0 left-1/2"
      style={{ backgroundColor: color }}
      initial={{ x: 0, y: 0, opacity: 1, rotate: 0 }}
      animate={{ x, y: 300, opacity: 0, rotate: Math.random() * 720 }}
      transition={{ duration: 1.5, delay, ease: 'easeOut' }}
    />
  )
}

export function TriviaChallenge({ config, onSubmit, demoMode = false }: TriviaChallengeProps) {
  const [currentIdx, setCurrentIdx] = useState(0)
  const [answers, setAnswers] = useState<number[]>([])
  const [selected, setSelected] = useState<number | null>(null)
  const [answered, setAnswered] = useState(false)
  const [gameState, setGameState] = useState<GameState>('playing')
  const [result, setResult] = useState<ChallengeResult | null>(null)
  const [timeLeft, setTimeLeft] = useState(config.timeLimitSeconds ?? 0)
  const [confetti] = useState(() => Array.from({ length: 30 }, (_, i) => i))
  const [isSubmitting, setIsSubmitting] = useState(false)

  const questions = config.questions
  const current: TriviaQuestion = questions[currentIdx]
  const progress = ((currentIdx) / questions.length) * 100
  const hasTimer = !!config.timeLimitSeconds

  const handleNextOrFinish = useCallback(
    async (finalAnswers: number[]) => {
      if (currentIdx < questions.length - 1) {
        setCurrentIdx((i) => i + 1)
        setSelected(null)
        setAnswered(false)
        if (hasTimer) setTimeLeft(config.timeLimitSeconds!)
      } else {
        if (demoMode) {
          const correct = finalAnswers.filter(
            (a, i) => questions[i]?.options[a]?.isCorrect
          ).length
          setResult({
            success: true,
            message: `¡Obtuviste ${correct}/${questions.length} respuestas correctas!`,
            pointsAwarded: correct * 100,
            prize: correct >= 2
              ? { id: 'demo', prizeId: 'demo', prizeName: '¡Bonus de puntos!', prizeType: 'Points', pointsAwarded: correct * 100, awardedAt: new Date().toISOString(), isRedeemed: false }
              : undefined,
          })
          setGameState(correct >= 2 ? 'prize' : 'result')
        } else if (onSubmit) {
          setIsSubmitting(true)
          try {
            const res = await onSubmit(finalAnswers)
            setResult(res)
            // Use server success to determine game state
            setGameState(res.success ? 'prize' : 'result')
          } finally {
            setIsSubmitting(false)
          }
        }
      }
    },
    [currentIdx, questions, demoMode, onSubmit, hasTimer, config.timeLimitSeconds]
  )

  // Timer
  useEffect(() => {
    if (!hasTimer || gameState !== 'playing' || answered) return
    if (timeLeft <= 0) {
      const next = [...answers, -1]
      setAnswers(next)
      setAnswered(true)
      setTimeout(() => handleNextOrFinish(next), 1000)
      return
    }
    const t = setTimeout(() => setTimeLeft((t) => t - 1), 1000)
    return () => clearTimeout(t)
  }, [timeLeft, hasTimer, gameState, answered, answers, handleNextOrFinish])

  useEffect(() => {
    if (hasTimer && !answered) setTimeLeft(config.timeLimitSeconds!)
  }, [currentIdx, hasTimer, config.timeLimitSeconds, answered])

  const handleSelect = (idx: number) => {
    if (answered) return
    setSelected(idx)
    setAnswered(true)
    const next = [...answers, idx]
    setAnswers(next)
    setTimeout(() => handleNextOrFinish(next), 800)
  }

  const reset = () => {
    setCurrentIdx(0)
    setAnswers([])
    setSelected(null)
    setAnswered(false)
    setGameState('playing')
    setResult(null)
    if (hasTimer) setTimeLeft(config.timeLimitSeconds!)
  }

  // Use server-side CorrectCount if available (opt.isCorrect is stripped from config for security)
  const serverCorrectCount = result?.payload?.CorrectCount
  const correctCount: number = typeof serverCorrectCount === 'number'
    ? serverCorrectCount
    : answers.filter((a, i) => questions[i]?.options[a]?.isCorrect).length

  return (
    <div className="relative">
      <AnimatePresence mode="wait">
        {gameState === 'playing' && (
          <motion.div
            key="playing"
            initial={{ opacity: 0, x: 30 }}
            animate={{ opacity: 1, x: 0 }}
            exit={{ opacity: 0, x: -30 }}
            className="space-y-6"
          >
            {/* Progress */}
            <div className="space-y-2">
              <div className="flex justify-between text-sm text-zinc-400">
                <span>Pregunta {currentIdx + 1} de {questions.length}</span>
                {hasTimer && (
                  <span className={cn('font-mono font-bold', timeLeft <= 5 ? 'text-red-400 animate-pulse' : 'text-violet-400')}>
                    ⏱ {timeLeft}s
                  </span>
                )}
              </div>
              <Progress value={progress} className="h-1.5 bg-white/5" />
            </div>

            {/* Question */}
            <div className="p-6 rounded-2xl bg-white/5 border border-white/10">
              <h3 className="text-xl font-bold text-white leading-relaxed">{current.question}</h3>
            </div>

            {/* Options */}
            <div className="grid grid-cols-1 gap-3">
              {current.options.map((opt, i) => {
                const isSelected = selected === i
                const showResult = answered
                const isCorrect = opt.isCorrect
                return (
                  <motion.button
                    key={i}
                    whileHover={!answered ? { scale: 1.01 } : {}}
                    whileTap={!answered ? { scale: 0.99 } : {}}
                    onClick={() => handleSelect(i)}
                    className={cn(
                      'text-left px-5 py-4 rounded-xl border text-sm font-medium transition-all duration-300',
                      !answered && 'bg-white/5 border-white/10 text-zinc-300 hover:bg-violet-500/10 hover:border-violet-500/40 hover:text-white cursor-pointer',
                      showResult && isCorrect && 'bg-emerald-500/20 border-emerald-500/50 text-emerald-300',
                      showResult && isSelected && !isCorrect && 'bg-red-500/20 border-red-500/50 text-red-300 animate-pulse',
                      showResult && !isSelected && !isCorrect && 'opacity-40 bg-white/5 border-white/5 text-zinc-500'
                    )}
                  >
                    <span className="flex items-center gap-3">
                      <span className={cn(
                        'w-7 h-7 rounded-lg flex items-center justify-center text-xs font-bold border flex-shrink-0',
                        showResult && isCorrect ? 'bg-emerald-500/30 border-emerald-500/50 text-emerald-300' :
                        showResult && isSelected && !isCorrect ? 'bg-red-500/30 border-red-500/50 text-red-300' :
                        'bg-white/10 border-white/10 text-zinc-400'
                      )}>
                        {['A', 'B', 'C', 'D'][i]}
                      </span>
                      {opt.text}
                    </span>
                  </motion.button>
                )
              })}
            </div>

            {isSubmitting && (
              <div className="text-center text-violet-400 text-sm animate-pulse">Enviando respuestas...</div>
            )}
          </motion.div>
        )}

        {gameState === 'result' && (
          <motion.div
            key="result"
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            className="text-center space-y-6 py-8"
          >
            <div className="text-6xl mb-4">🧠</div>
            <h2 className="text-2xl font-bold text-white">
              ¡Obtuviste {correctCount}/{questions.length} correctas!
            </h2>
            <div className="max-w-xs mx-auto space-y-2">
              <div className="flex justify-between text-sm text-zinc-400">
                <span>Score</span>
                <span className="text-emerald-400 font-bold">{Math.round((correctCount / questions.length) * 100)}%</span>
              </div>
              <Progress value={(correctCount / questions.length) * 100} className="h-3 bg-white/5" />
            </div>
            {result?.pointsAwarded && (
              <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-violet-500/20 border border-violet-500/30 text-violet-300 font-bold">
                <Star className="w-4 h-4" />
                +{result.pointsAwarded} puntos
              </div>
            )}
            <Button onClick={reset} variant="outline" className="gap-2 border-white/10 text-zinc-300 hover:bg-white/5">
              <RotateCcw className="w-4 h-4" />
              Intentar de nuevo
            </Button>
          </motion.div>
        )}

        {gameState === 'prize' && (
          <motion.div
            key="prize"
            initial={{ opacity: 0, scale: 0.8 }}
            animate={{ opacity: 1, scale: 1 }}
            className="text-center space-y-6 py-8 relative"
          >
            {/* Confetti */}
            <div className="absolute inset-0 pointer-events-none overflow-hidden">
              {confetti.map((i) => <ConfettiPiece key={i} index={i} />)}
            </div>

            <motion.div
              animate={{ rotate: [0, -10, 10, -5, 5, 0], scale: [1, 1.2, 1] }}
              transition={{ duration: 0.6, delay: 0.2 }}
              className="text-7xl"
            >
              🏆
            </motion.div>

            <div>
              <h2 className="text-3xl font-bold text-white mb-2">¡Premio ganado!</h2>
              <p className="text-zinc-400">Excelente desempeño</p>
            </div>

            {result?.prize && (
              <motion.div
                initial={{ y: 20, opacity: 0 }}
                animate={{ y: 0, opacity: 1 }}
                transition={{ delay: 0.4 }}
                className="max-w-sm mx-auto p-6 rounded-2xl bg-gradient-to-br from-violet-600/20 to-blue-600/20 border border-violet-500/30"
              >
                <Trophy className="w-8 h-8 text-violet-400 mx-auto mb-3" />
                <p className="font-bold text-white text-lg">{result.prize.prizeName}</p>
                {result.prize.pointsAwarded > 0 && (
                  <p className="text-violet-400 font-mono font-bold text-2xl mt-2">
                    +{result.prize.pointsAwarded} pts
                  </p>
                )}
              </motion.div>
            )}

            <div className="flex gap-3 justify-center">
              <Button
                onClick={reset}
                variant="outline"
                className="gap-2 border-white/10 text-zinc-300 hover:bg-white/5"
              >
                <RotateCcw className="w-4 h-4" />
                Jugar otra vez
              </Button>
              <Button className="gap-2 bg-violet-600 hover:bg-violet-500">
                <Trophy className="w-4 h-4" />
                Ver mis premios
                <ChevronRight className="w-4 h-4" />
              </Button>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}

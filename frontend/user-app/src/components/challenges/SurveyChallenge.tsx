import { useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { Progress } from '@/components/ui/progress'
import { Button } from '@/components/ui/button'
import { ChevronLeft, ChevronRight, Star, CheckCircle } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { SurveyConfig, ChallengeResult } from '@/types/api'

interface SurveyChallengeProps {
  config: SurveyConfig
  onSubmit?: (answers: Record<string, unknown>) => Promise<ChallengeResult>
  demoMode?: boolean
}

function StarRating({ value, onChange }: { value: number; onChange: (v: number) => void }) {
  const [hovered, setHovered] = useState(0)
  return (
    <div className="flex gap-2 justify-center py-4">
      {[1, 2, 3, 4, 5].map((star) => (
        <button
          key={star}
          type="button"
          onMouseEnter={() => setHovered(star)}
          onMouseLeave={() => setHovered(0)}
          onClick={() => onChange(star)}
          className="transition-transform hover:scale-110"
        >
          <Star
            className={cn(
              'w-10 h-10 transition-colors',
              (hovered || value) >= star ? 'fill-amber-400 text-amber-400' : 'text-zinc-600'
            )}
          />
        </button>
      ))}
    </div>
  )
}

export function SurveyChallenge({ config, onSubmit, demoMode = false }: SurveyChallengeProps) {
  const [currentIdx, setCurrentIdx] = useState(0)
  const [answers, setAnswers] = useState<Record<string, unknown>>({})
  const [done, setDone] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const questions = config.questions
  const current = questions[currentIdx]
  const progress = ((currentIdx + 1) / questions.length) * 100
  const currentAnswer = answers[current?.id ?? '']

  const setAnswer = (val: unknown) => {
    setAnswers((prev) => ({ ...prev, [current.id]: val }))
  }

  const handleNext = async () => {
    if (currentIdx < questions.length - 1) {
      setCurrentIdx((i) => i + 1)
    } else {
      if (demoMode) {
        setDone(true)
      } else if (onSubmit) {
        setIsSubmitting(true)
        try {
          await onSubmit(answers)
          setDone(true)
        } finally {
          setIsSubmitting(false)
        }
      }
    }
  }

  if (done) {
    return (
      <motion.div
        initial={{ opacity: 0, scale: 0.9 }}
        animate={{ opacity: 1, scale: 1 }}
        className="text-center space-y-6 py-12"
      >
        <div className="w-20 h-20 rounded-full bg-emerald-500/20 flex items-center justify-center mx-auto border border-emerald-500/30">
          <CheckCircle className="w-10 h-10 text-emerald-400" />
        </div>
        <h2 className="text-2xl font-bold text-white">¡Gracias por tu opinión!</h2>
        <p className="text-zinc-400">Tus respuestas han sido registradas exitosamente.</p>
      </motion.div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Progress */}
      <div className="space-y-2">
        <div className="flex justify-between text-sm text-zinc-400">
          <span>Pregunta {currentIdx + 1} de {questions.length}</span>
          <span className="text-violet-400">{Math.round(progress)}%</span>
        </div>
        <Progress value={progress} className="h-1.5 bg-white/5" />
      </div>

      {/* Question */}
      <AnimatePresence mode="wait">
        <motion.div
          key={currentIdx}
          initial={{ opacity: 0, x: 20 }}
          animate={{ opacity: 1, x: 0 }}
          exit={{ opacity: 0, x: -20 }}
          className="space-y-4"
        >
          <div className="p-6 rounded-2xl bg-white/5 border border-white/10">
            <p className="text-zinc-400 text-xs font-medium uppercase tracking-wider mb-2">
              {current.required ? 'Requerida' : 'Opcional'}
            </p>
            <h3 className="text-xl font-bold text-white">{current.question}</h3>
          </div>

          {/* Input by type */}
          {current.type === 'text' && (
            <textarea
              value={(currentAnswer as string) ?? ''}
              onChange={(e) => setAnswer(e.target.value)}
              placeholder="Escribe tu respuesta aquí..."
              rows={4}
              className="w-full px-4 py-3 rounded-xl bg-white/5 border border-white/10 text-white placeholder-zinc-600 focus:border-violet-500/50 focus:outline-none focus:bg-violet-500/5 resize-none transition-all"
            />
          )}

          {current.type === 'rating' && (
            <StarRating
              value={(currentAnswer as number) ?? 0}
              onChange={setAnswer}
            />
          )}

          {current.type === 'multiple_choice' && current.options && (
            <div className="space-y-2">
              {current.options.map((opt, i) => (
                <button
                  key={i}
                  onClick={() => setAnswer(opt)}
                  className={cn(
                    'w-full text-left px-4 py-3 rounded-xl border text-sm font-medium transition-all',
                    currentAnswer === opt
                      ? 'bg-violet-500/20 border-violet-500/50 text-violet-300'
                      : 'bg-white/5 border-white/10 text-zinc-300 hover:bg-white/10 hover:border-white/20'
                  )}
                >
                  {opt}
                </button>
              ))}
            </div>
          )}
        </motion.div>
      </AnimatePresence>

      {/* Navigation */}
      <div className="flex justify-between pt-2">
        <Button
          variant="outline"
          onClick={() => setCurrentIdx((i) => i - 1)}
          disabled={currentIdx === 0}
          className="gap-2 border-white/10 text-zinc-300 hover:bg-white/5 disabled:opacity-30"
        >
          <ChevronLeft className="w-4 h-4" />
          Anterior
        </Button>
        <Button
          onClick={handleNext}
          disabled={(current.required && !currentAnswer) || isSubmitting}
          className="gap-2 bg-violet-600 hover:bg-violet-500"
        >
          {currentIdx < questions.length - 1 ? 'Siguiente' : isSubmitting ? 'Enviando...' : 'Finalizar'}
          <ChevronRight className="w-4 h-4" />
        </Button>
      </div>
    </div>
  )
}

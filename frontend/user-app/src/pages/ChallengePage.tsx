import { useParams, useNavigate } from 'react-router-dom'
import { motion } from 'framer-motion'
import { ArrowLeft, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useChallenge } from '@/hooks/useChallenge'
import { useSubmitChallenge } from '@/hooks/useSubmitChallenge'
import { TriviaChallenge } from '@/components/challenges/TriviaChallenge'
import { SurveyChallenge } from '@/components/challenges/SurveyChallenge'
import { CodeChallenge } from '@/components/challenges/CodeChallenge'
import type { TriviaConfig, SurveyConfig } from '@/types/api'

export default function ChallengePage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: challenge, isLoading, error } = useChallenge(id ?? '')
  const submitMutation = useSubmitChallenge(id ?? '')

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="w-8 h-8 text-violet-400 animate-spin" />
      </div>
    )
  }

  if (error || !challenge) {
    return (
      <div className="text-center py-20">
        <p className="text-zinc-500 mb-4">No se encontró el desafío</p>
        <Button variant="outline" onClick={() => navigate('/')} className="border-white/10 text-zinc-400">
          Volver al inicio
        </Button>
      </div>
    )
  }

  const config = challenge.configurationJson
    ? JSON.parse(challenge.configurationJson)
    : {}

  return (
    <motion.div
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      className="max-w-2xl mx-auto"
    >
      {/* Header */}
      <div className="flex items-center gap-3 mb-8">
        <Button
          variant="ghost"
          size="icon"
          onClick={() => navigate(-1)}
          className="text-zinc-400 hover:text-zinc-200 hover:bg-white/5"
        >
          <ArrowLeft className="w-5 h-5" />
        </Button>
        <div>
          <h1 className="text-xl font-bold text-white">{challenge.name}</h1>
          {challenge.description && (
            <p className="text-sm text-zinc-500 mt-0.5">{challenge.description}</p>
          )}
        </div>
      </div>

      {/* Challenge renderer */}
      <div className="bg-white/5 border border-white/10 rounded-2xl p-6">
        {challenge.type === 'Trivia' && (
          <TriviaChallenge
            config={config as TriviaConfig}
            onSubmit={(answers) =>
              submitMutation.mutateAsync({
                inputs: Object.fromEntries(answers.map((a, i) => [`q${i}`, String(a)])),
              })
            }
          />
        )}
        {challenge.type === 'Survey' && (
          <SurveyChallenge
            config={config as SurveyConfig}
            onSubmit={(answers) =>
              submitMutation.mutateAsync({
                inputs: Object.fromEntries(
                  Object.entries(answers).map(([k, v]) => [k, String(v ?? '')])
                ),
              })
            }
          />
        )}
        {challenge.type === 'Code' && (
          <CodeChallenge
            onSubmit={(code) =>
              submitMutation.mutateAsync({ inputs: { code } })
            }
          />
        )}
        {challenge.type === 'Rally' && (
          <div className="text-center py-12 text-zinc-500">
            <p className="text-lg font-medium">Rally Challenge</p>
            <p className="text-sm mt-1">Próximamente disponible</p>
          </div>
        )}
      </div>
    </motion.div>
  )
}

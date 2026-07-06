import { motion } from 'framer-motion'
import { useNavigate } from 'react-router-dom'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { CheckCircle, Lock, Zap, Brain, Mic, Code2, Play } from 'lucide-react'
import type { DynamicChallenge } from '@/types/api'
import { cn } from '@/lib/utils'

interface ChallengeCardProps {
  challenge: DynamicChallenge
  completed?: boolean
}

const typeConfig = {
  Trivia: { label: 'Trivia', color: 'bg-blue-500/20 text-blue-300 border-blue-500/30', icon: Brain, glow: 'shadow-blue-500/20' },
  Survey: { label: 'Encuesta', color: 'bg-green-500/20 text-green-300 border-green-500/30', icon: Mic, glow: 'shadow-green-500/20' },
  Code: { label: 'Código', color: 'bg-amber-500/20 text-amber-300 border-amber-500/30', icon: Code2, glow: 'shadow-amber-500/20' },
  Rally: { label: 'Rally', color: 'bg-violet-500/20 text-violet-300 border-violet-500/30', icon: Zap, glow: 'shadow-violet-500/20' },
}

export function ChallengeCard({ challenge, completed = false }: ChallengeCardProps) {
  const navigate = useNavigate()
  const config = typeConfig[challenge.type] ?? typeConfig.Rally
  const TypeIcon = config.icon

  return (
    <motion.div
      whileHover={{ scale: 1.02, y: -2 }}
      transition={{ type: 'spring', stiffness: 300, damping: 20 }}
      className={cn(
        'group relative rounded-2xl p-5 bg-white/5 backdrop-blur-sm',
        'border border-white/10 hover:border-violet-500/40',
        'shadow-lg hover:shadow-xl hover:shadow-violet-500/10',
        'transition-all duration-300 cursor-pointer',
        !challenge.isActive && !completed && 'opacity-60'
      )}
      onClick={() => challenge.isActive && !completed && navigate(`/challenge/${challenge.id}`)}
    >
      {/* Glow effect on hover */}
      <div className="absolute inset-0 rounded-2xl opacity-0 group-hover:opacity-100 transition-opacity duration-300 bg-gradient-to-br from-violet-600/5 to-blue-600/5 pointer-events-none" />

      {/* Type badge + status */}
      <div className="flex items-start justify-between mb-4">
        <Badge className={cn('text-xs font-semibold border', config.color)}>
          <TypeIcon className="w-3 h-3 mr-1" />
          {config.label}
        </Badge>

        {completed ? (
          <div className="flex items-center gap-1 text-emerald-400 text-xs font-medium">
            <CheckCircle className="w-4 h-4" />
            Completado
          </div>
        ) : !challenge.isActive ? (
          <div className="flex items-center gap-1 text-zinc-500 text-xs">
            <Lock className="w-3 h-3" />
            Bloqueado
          </div>
        ) : null}
      </div>

      {/* Content */}
      <h3 className="font-bold text-zinc-100 text-base mb-2 leading-tight group-hover:text-white transition-colors">
        {challenge.name}
      </h3>

      {challenge.description && (
        <p className="text-sm text-zinc-500 mb-4 line-clamp-2 leading-relaxed">
          {challenge.description}
        </p>
      )}

      {/* CTA */}
      {challenge.isActive && !completed && (
        <Button
          size="sm"
          className="w-full bg-violet-600 hover:bg-violet-500 text-white gap-2 group-hover:shadow-lg group-hover:shadow-violet-500/25 transition-all"
          onClick={(e) => {
            e.stopPropagation()
            navigate(`/challenge/${challenge.id}`)
          }}
        >
          <Play className="w-3 h-3" />
          Participar
        </Button>
      )}
    </motion.div>
  )
}

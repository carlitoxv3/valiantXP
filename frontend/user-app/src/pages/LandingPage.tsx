import { motion } from 'framer-motion'
import { Zap, ChevronRight, Trophy, Users, Star } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { ChallengeCard } from '@/components/challenges/ChallengeCard'
import { useCampaigns } from '@/hooks/useCampaigns'
import { useAuthStore } from '@/stores/authStore'
import { useNavigate } from 'react-router-dom'
import type { DynamicChallenge } from '@/types/api'

const stats = [
  { icon: Trophy, label: 'Premios entregados', value: '12,450' },
  { icon: Users, label: 'Participantes activos', value: '3,200' },
  { icon: Star, label: 'Puntos canjeados', value: '890K' },
]

export default function LandingPage() {
  const navigate = useNavigate()
  const user = useAuthStore((s) => s.user)
  const { data: campaigns, isLoading } = useCampaigns()

  const allChallenges: DynamicChallenge[] = campaigns?.flatMap((c) => c.challenges) ?? []

  return (
    <div className="space-y-10">
      {/* Hero */}
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        className="relative rounded-3xl overflow-hidden bg-gradient-to-br from-violet-600/20 via-purple-900/10 to-blue-600/20 border border-white/10 p-10"
      >
        <div className="absolute inset-0 bg-gradient-to-br from-violet-600/5 to-blue-600/5 pointer-events-none" />
        <div className="absolute top-0 right-0 w-64 h-64 bg-violet-500/10 rounded-full blur-3xl pointer-events-none" />

        <div className="relative z-10 max-w-2xl">
          <div className="flex items-center gap-2 mb-4">
            <div className="px-3 py-1 rounded-full bg-violet-500/20 border border-violet-500/30 text-violet-300 text-xs font-semibold flex items-center gap-1.5">
              <Zap className="w-3 h-3" />
              En vivo
            </div>
          </div>

          <h1 className="text-4xl md:text-5xl font-black text-white leading-tight mb-4">
            Participa.{' '}
            <span className="bg-gradient-to-r from-violet-400 to-blue-400 bg-clip-text text-transparent">
              Gana.
            </span>{' '}
            Celebra.
          </h1>

          <p className="text-zinc-400 text-lg mb-8 max-w-lg">
            {user
              ? `Hola, ${user.displayName ?? user.email}. Hay desafíos esperándote.`
              : 'Completa desafíos, gana premios exclusivos y acumula puntos en cada campaña.'}
          </p>

          <div className="flex gap-3">
            <Button
              onClick={() => document.getElementById('challenges')?.scrollIntoView({ behavior: 'smooth' })}
              className="bg-violet-600 hover:bg-violet-500 shadow-lg shadow-violet-500/25 gap-2"
            >
              Ver desafíos
              <ChevronRight className="w-4 h-4" />
            </Button>
            <Button
              variant="outline"
              onClick={() => navigate('/demo')}
              className="border-white/10 text-zinc-300 hover:bg-white/5 gap-2"
            >
              Ver demo
            </Button>
          </div>
        </div>

        {/* Stats */}
        <div className="relative z-10 grid grid-cols-3 gap-4 mt-10 pt-8 border-t border-white/10">
          {stats.map(({ icon: Icon, label, value }) => (
            <div key={label} className="text-center">
              <div className="flex items-center justify-center gap-2 mb-1">
                <Icon className="w-4 h-4 text-violet-400" />
                <span className="text-2xl font-black text-white">{value}</span>
              </div>
              <p className="text-xs text-zinc-500">{label}</p>
            </div>
          ))}
        </div>
      </motion.div>

      {/* Challenges grid */}
      <section id="challenges">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-xl font-bold text-white">Desafíos activos</h2>
            <p className="text-sm text-zinc-500 mt-0.5">Participa y gana premios</p>
          </div>
          {allChallenges.length > 0 && (
            <span className="text-xs text-zinc-600 font-medium">
              {allChallenges.filter((c) => c.isActive).length} disponibles
            </span>
          )}
        </div>

        {isLoading ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="h-48 rounded-2xl bg-white/5 animate-pulse border border-white/5" />
            ))}
          </div>
        ) : allChallenges.length === 0 ? (
          <div className="text-center py-20 text-zinc-600">
            <Trophy className="w-16 h-16 mx-auto mb-4 opacity-30" />
            <p className="text-lg font-medium">No hay desafíos activos</p>
            <p className="text-sm mt-1">Vuelve pronto para nuevas campañas</p>
          </div>
        ) : (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ staggerChildren: 0.08 }}
            className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4"
          >
            {allChallenges.map((challenge, i) => (
              <motion.div
                key={challenge.id}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: i * 0.06 }}
              >
                <ChallengeCard challenge={challenge} />
              </motion.div>
            ))}
          </motion.div>
        )}
      </section>
    </div>
  )
}

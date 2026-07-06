import { motion } from 'framer-motion'
import { Trophy, Gift, CreditCard, Coins, Calendar, CheckCircle } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import type { UserPrize } from '@/types/api'
import { cn } from '@/lib/utils'

interface PrizeCardProps {
  prize: UserPrize
  onReveal?: () => void
}

const typeConfig = {
  Points: {
    icon: Coins,
    color: 'text-amber-400',
    bg: 'bg-amber-500/10 border-amber-500/20',
    label: 'Puntos',
    badgeClass: 'bg-amber-500/20 text-amber-300 border-amber-500/30',
  },
  Product: {
    icon: Gift,
    color: 'text-blue-400',
    bg: 'bg-blue-500/10 border-blue-500/20',
    label: 'Producto',
    badgeClass: 'bg-blue-500/20 text-blue-300 border-blue-500/30',
  },
  GiftCard: {
    icon: CreditCard,
    color: 'text-violet-400',
    bg: 'bg-violet-500/10 border-violet-500/20',
    label: 'Gift Card',
    badgeClass: 'bg-violet-500/20 text-violet-300 border-violet-500/30',
  },
}

export function PrizeCard({ prize, onReveal }: PrizeCardProps) {
  const config = typeConfig[prize.prizeType]
  const Icon = config.icon
  const date = new Date(prize.awardedAt).toLocaleDateString('es-ES', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  })

  return (
    <motion.div
      whileHover={{ y: -2 }}
      className={cn(
        'rounded-2xl p-5 border',
        config.bg,
        'hover:shadow-lg transition-all duration-300'
      )}
    >
      <div className="flex items-start justify-between mb-4">
        <div className={cn('w-10 h-10 rounded-xl flex items-center justify-center', 'bg-white/10')}>
          <Icon className={cn('w-5 h-5', config.color)} />
        </div>
        <div className="flex flex-col items-end gap-1">
          <Badge className={cn('text-xs border', config.badgeClass)}>{config.label}</Badge>
          {prize.isRedeemed && (
            <div className="flex items-center gap-1 text-emerald-400 text-xs">
              <CheckCircle className="w-3 h-3" />
              Canjeado
            </div>
          )}
        </div>
      </div>

      <h3 className="font-bold text-white text-base mb-1">{prize.prizeName}</h3>

      {prize.prizeType === 'Points' && prize.pointsAwarded > 0 && (
        <p className={cn('text-2xl font-mono font-bold', config.color)}>
          +{prize.pointsAwarded.toLocaleString()} pts
        </p>
      )}

      <div className="flex items-center gap-1.5 mt-3 text-xs text-zinc-500">
        <Calendar className="w-3 h-3" />
        {date}
      </div>

      {prize.prizeType === 'GiftCard' && !prize.isRedeemed && (
        <button
          onClick={onReveal}
          className={cn(
            'mt-4 w-full py-2.5 rounded-xl text-sm font-bold transition-all',
            'bg-violet-600 hover:bg-violet-500 text-white shadow-lg shadow-violet-500/20',
            'hover:shadow-violet-500/40'
          )}
        >
          🎁 Revelar código
        </button>
      )}

      {prize.prizeType === 'GiftCard' && prize.giftCardCode && prize.isRedeemed && (
        <div className="mt-4 p-3 rounded-xl bg-white/5 border border-white/10">
          <p className="text-xs text-zinc-500 mb-1">Código revelado</p>
          <code className="text-violet-300 font-mono text-sm tracking-wider">{prize.giftCardCode}</code>
        </div>
      )}

      {prize.expiresAt && (
        <p className="mt-2 text-xs text-zinc-600">
          Expira: {new Date(prize.expiresAt).toLocaleDateString('es-ES')}
        </p>
      )}
    </motion.div>
  )
}

// Also export a Trophy icon wrapper for empty state
export function PrizeTrophyIcon() {
  return <Trophy className="w-16 h-16 text-zinc-700 mx-auto" />
}

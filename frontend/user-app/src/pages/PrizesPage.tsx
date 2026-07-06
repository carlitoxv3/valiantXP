import { useState } from 'react'
import { motion } from 'framer-motion'
import { Trophy, Coins, Gift, CreditCard, Loader2 } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { PrizeCard } from '@/components/prizes/PrizeCard'
import { GiftCardReveal } from '@/components/prizes/GiftCardReveal'
import { useMyPrizes } from '@/hooks/useMyPrizes'
import { useCurrentUser } from '@/hooks/useCurrentUser'
import { Dialog, DialogContent } from '@/components/ui/dialog'
import type { UserPrize } from '@/types/api'

const typeFilters = ['Todos', 'Points', 'Product', 'GiftCard'] as const
type Filter = (typeof typeFilters)[number]

export default function PrizesPage() {
  const { data: prizes, isLoading } = useMyPrizes()
  const { data: user } = useCurrentUser()
  const [filter, setFilter] = useState<Filter>('Todos')
  const [revealPrize, setRevealPrize] = useState<UserPrize | null>(null)

  const filtered = prizes?.filter((p) => filter === 'Todos' || p.prizeType === filter) ?? []

  return (
    <motion.div initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} className="space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold text-white">Mis Premios</h1>
          <p className="text-zinc-500 text-sm mt-0.5">Premios y recompensas ganados</p>
        </div>
        {user && (
          <Badge className="bg-violet-500/20 text-violet-300 border border-violet-500/30 text-sm px-4 py-1.5">
            <Coins className="w-3.5 h-3.5 mr-2" />
            {user.totalPoints.toLocaleString()} puntos
          </Badge>
        )}
      </div>

      {/* Filter tabs */}
      <div className="flex gap-2 flex-wrap">
        {typeFilters.map((f) => (
          <button
            key={f}
            onClick={() => setFilter(f)}
            className={`px-4 py-2 rounded-xl text-sm font-medium transition-all ${
              filter === f
                ? 'bg-violet-600 text-white shadow-lg shadow-violet-500/25'
                : 'bg-white/5 text-zinc-400 border border-white/10 hover:bg-white/10 hover:text-zinc-200'
            }`}
          >
            {f === 'Todos' ? 'Todos' : f === 'Points' ? '💰 Puntos' : f === 'Product' ? '🎁 Productos' : '💳 Gift Cards'}
          </button>
        ))}
      </div>

      {/* Content */}
      {isLoading ? (
        <div className="flex justify-center py-20">
          <Loader2 className="w-8 h-8 text-violet-400 animate-spin" />
        </div>
      ) : filtered.length === 0 ? (
        <div className="text-center py-20">
          <div className="w-20 h-20 rounded-2xl bg-white/5 border border-white/10 flex items-center justify-center mx-auto mb-4">
            <Trophy className="w-10 h-10 text-zinc-700" />
          </div>
          <p className="text-zinc-500 text-lg font-medium">Sin premios aún</p>
          <p className="text-zinc-600 text-sm mt-1">Participa en desafíos para ganar recompensas</p>
        </div>
      ) : (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4"
        >
          {filtered.map((prize, i) => (
            <motion.div
              key={prize.id}
              initial={{ opacity: 0, y: 16 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: i * 0.05 }}
            >
              <PrizeCard
                prize={prize}
                onReveal={() => setRevealPrize(prize)}
              />
            </motion.div>
          ))}
        </motion.div>
      )}

      {/* Stats bar */}
      {prizes && prizes.length > 0 && (
        <div className="grid grid-cols-3 gap-4 pt-4 border-t border-white/5">
          {[
            { icon: Coins, label: 'Puntos ganados', value: prizes.filter(p => p.prizeType === 'Points').length },
            { icon: Gift, label: 'Productos', value: prizes.filter(p => p.prizeType === 'Product').length },
            { icon: CreditCard, label: 'Gift Cards', value: prizes.filter(p => p.prizeType === 'GiftCard').length },
          ].map(({ icon: Icon, label, value }) => (
            <div key={label} className="text-center p-4 rounded-xl bg-white/5 border border-white/5">
              <Icon className="w-5 h-5 text-zinc-500 mx-auto mb-2" />
              <p className="text-2xl font-bold text-white">{value}</p>
              <p className="text-xs text-zinc-600 mt-0.5">{label}</p>
            </div>
          ))}
        </div>
      )}

      {/* Gift card reveal modal */}
      <Dialog open={!!revealPrize} onOpenChange={() => setRevealPrize(null)}>
        <DialogContent className="bg-zinc-900 border border-white/10 rounded-2xl max-w-sm">
          {revealPrize && (
            <GiftCardReveal
              code={revealPrize.giftCardCode ?? 'CODE-XXXX'}
              redeemUrl={revealPrize.giftCardRedeemUrl}
              prizeName={revealPrize.prizeName}
            />
          )}
        </DialogContent>
      </Dialog>
    </motion.div>
  )
}

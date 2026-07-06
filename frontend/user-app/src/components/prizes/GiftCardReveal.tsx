import { useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { Copy, Check, ExternalLink } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

interface GiftCardRevealProps {
  code: string
  redeemUrl?: string
  prizeName: string
}

type RevealState = 'hidden' | 'revealing' | 'revealed'

function ScratchParticle({ index }: { index: number }) {
  const angle = (index / 16) * Math.PI * 2
  const radius = 60 + Math.random() * 40
  return (
    <motion.div
      className="absolute w-1.5 h-1.5 rounded-full bg-violet-400 top-1/2 left-1/2"
      initial={{ x: 0, y: 0, opacity: 1, scale: 1 }}
      animate={{
        x: Math.cos(angle) * radius,
        y: Math.sin(angle) * radius,
        opacity: 0,
        scale: 0,
      }}
      transition={{ duration: 0.6, delay: index * 0.02, ease: 'easeOut' }}
    />
  )
}

export function GiftCardReveal({ code, redeemUrl, prizeName }: GiftCardRevealProps) {
  const [revealState, setRevealState] = useState<RevealState>('hidden')
  const [copied, setCopied] = useState(false)
  const [particles] = useState(() => Array.from({ length: 16 }, (_, i) => i))

  const handleReveal = () => {
    setRevealState('revealing')
    setTimeout(() => setRevealState('revealed'), 800)
  }

  const handleCopy = async () => {
    await navigator.clipboard.writeText(code)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <div className="space-y-4">
      <h3 className="text-sm font-semibold text-zinc-300 text-center">{prizeName}</h3>

      <div className="relative flex items-center justify-center min-h-[120px]">
        {/* Particles */}
        {revealState === 'revealing' && (
          <div className="absolute inset-0 pointer-events-none overflow-hidden flex items-center justify-center">
            {particles.map((i) => <ScratchParticle key={i} index={i} />)}
          </div>
        )}

        <AnimatePresence mode="wait">
          {revealState === 'hidden' && (
            <motion.button
              key="hidden"
              exit={{ scale: 0, opacity: 0 }}
              onClick={handleReveal}
              className="px-8 py-6 rounded-2xl bg-gradient-to-br from-violet-600/30 to-blue-600/30 border-2 border-dashed border-violet-500/40 text-violet-300 font-semibold text-lg hover:from-violet-600/40 hover:to-blue-600/40 transition-all hover:scale-105 active:scale-95"
            >
              🎁 Revelar código
            </motion.button>
          )}

          {revealState === 'revealing' && (
            <motion.div
              key="revealing"
              initial={{ scale: 1 }}
              animate={{ scale: [1, 1.1, 0.9, 1], rotate: [0, -3, 3, 0] }}
              transition={{ duration: 0.6 }}
              className="px-8 py-6 rounded-2xl bg-violet-600/20 border-2 border-violet-500/40"
            >
              <div className="w-8 h-8 border-2 border-violet-400 border-t-transparent rounded-full animate-spin mx-auto" />
            </motion.div>
          )}

          {revealState === 'revealed' && (
            <motion.div
              key="revealed"
              initial={{ scale: 0.8, opacity: 0 }}
              animate={{ scale: 1, opacity: 1 }}
              transition={{ type: 'spring', stiffness: 200, damping: 15 }}
              className="w-full"
            >
              <div className={cn(
                'p-6 rounded-2xl text-center',
                'bg-gradient-to-br from-violet-600/20 via-purple-600/10 to-blue-600/20',
                'border border-violet-400/40 shadow-xl shadow-violet-500/20',
              )}>
                <p className="text-xs text-zinc-500 mb-3 uppercase tracking-widest">Tu código</p>
                <code className="text-violet-200 font-mono text-2xl font-bold tracking-[0.3em] block mb-4 text-shadow">
                  {code}
                </code>
                <div className="flex gap-2 justify-center">
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={handleCopy}
                    className={cn(
                      'gap-2 border-violet-500/40 hover:bg-violet-500/20 transition-all',
                      copied ? 'text-emerald-400 border-emerald-500/40' : 'text-violet-300'
                    )}
                  >
                    {copied ? <Check className="w-4 h-4" /> : <Copy className="w-4 h-4" />}
                    {copied ? '¡Copiado!' : 'Copiar'}
                  </Button>
                  {redeemUrl && (
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => window.open(redeemUrl, '_blank')}
                      className="gap-2 border-white/10 text-zinc-400 hover:bg-white/5"
                    >
                      <ExternalLink className="w-4 h-4" />
                      Canjear
                    </Button>
                  )}
                </div>
              </div>
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </div>
  )
}

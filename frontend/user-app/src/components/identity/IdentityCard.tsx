import { useState } from 'react'
import { motion } from 'framer-motion'
import { api } from '@/lib/api'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { CheckCircle, AlertCircle, Unlink, ExternalLink } from 'lucide-react'
import type { UserIdentity } from '@/types/api'
import { cn } from '@/lib/utils'

interface IdentityCardProps {
  identity: UserIdentity
  canDisconnect: boolean
  onDisconnected?: () => void
}

const providerConfig = {
  Google: {
    color: 'text-blue-400',
    bg: 'bg-blue-500/10 border-blue-500/20',
    icon: (
      <svg viewBox="0 0 24 24" className="w-5 h-5">
        <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4" />
        <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853" />
        <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05" />
        <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335" />
      </svg>
    ),
  },
  Spotify: {
    color: 'text-green-400',
    bg: 'bg-green-500/10 border-green-500/20',
    icon: <svg viewBox="0 0 24 24" className="w-5 h-5" fill="#1DB954"><path d="M12 0C5.4 0 0 5.4 0 12s5.4 12 12 12 12-5.4 12-12S18.66 0 12 0zm5.521 17.34c-.24.359-.66.48-1.021.24-2.82-1.74-6.36-2.101-10.561-1.141-.418.122-.779-.179-.899-.539-.12-.421.18-.78.54-.9 4.56-1.021 8.52-.6 11.64 1.32.42.18.479.659.301 1.02zm1.44-3.3c-.301.42-.841.6-1.262.3-3.239-1.98-8.159-2.58-11.939-1.38-.479.12-1.02-.12-1.14-.6-.12-.48.12-1.021.6-1.141C9.6 9.9 15 10.561 18.72 12.84c.361.181.54.78.241 1.2zm.12-3.36C15.24 8.4 8.82 8.16 5.16 9.301c-.6.179-1.2-.181-1.38-.721-.18-.601.18-1.2.72-1.381 4.26-1.26 11.28-1.02 15.721 1.621.539.3.719 1.02.419 1.56-.299.421-1.02.599-1.559.3z"/></svg>,
  },
  Twitch: {
    color: 'text-purple-400',
    bg: 'bg-purple-500/10 border-purple-500/20',
    icon: <svg viewBox="0 0 24 24" className="w-5 h-5" fill="#9146FF"><path d="M11.571 4.714h1.715v5.143H11.57zm4.715 0H18v5.143h-1.714zM6 0L1.714 4.286v15.428h5.143V24l4.286-4.286h3.428L22.286 12V0zm14.571 11.143l-3.428 3.428h-3.429l-3 3v-3H6.857V1.714h13.714z"/></svg>,
  },
  EmailOtp: {
    color: 'text-zinc-300',
    bg: 'bg-zinc-500/10 border-zinc-500/20',
    icon: <span className="text-xl">📧</span>,
  },
  WhatsApp: {
    color: 'text-green-400',
    bg: 'bg-green-500/10 border-green-500/20',
    icon: <span className="text-xl">💬</span>,
  },
  Telegram: {
    color: 'text-blue-400',
    bg: 'bg-blue-500/10 border-blue-500/20',
    icon: <span className="text-xl">✈️</span>,
  },
}

export function IdentityCard({ identity, canDisconnect, onDisconnected }: IdentityCardProps) {
  const [confirming, setConfirming] = useState(false)
  const [loading, setLoading] = useState(false)
  const config = providerConfig[identity.provider]

  const handleDisconnect = async () => {
    if (!confirming) { setConfirming(true); return }
    setLoading(true)
    try {
      await api.delete(`/users/me/identities/${identity.id}`)
      onDisconnected?.()
    } finally {
      setLoading(false)
      setConfirming(false)
    }
  }

  return (
    <motion.div
      layout
      className={cn('flex items-center gap-4 p-4 rounded-xl border', config.bg)}
    >
      <div className="w-10 h-10 rounded-xl bg-white/10 flex items-center justify-center flex-shrink-0">
        {config.icon}
      </div>

      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 mb-0.5">
          <span className={cn('font-semibold text-sm', config.color)}>{identity.provider}</span>
          {identity.isPrimary && (
            <Badge className="text-xs bg-violet-500/20 text-violet-300 border-violet-500/30 border">Principal</Badge>
          )}
        </div>
        {identity.emailClaim && (
          <p className="text-xs text-zinc-500 truncate">{identity.emailClaim}</p>
        )}
        <div className="flex items-center gap-1 mt-1">
          {identity.isEmailVerified ? (
            <div className="flex items-center gap-1 text-emerald-400 text-xs">
              <CheckCircle className="w-3 h-3" />
              Verificado
            </div>
          ) : (
            <div className="flex items-center gap-1 text-amber-400 text-xs">
              <AlertCircle className="w-3 h-3" />
              Sin verificar
            </div>
          )}
        </div>
      </div>

      {canDisconnect && identity.isActive && (
        <Button
          variant="ghost"
          size="sm"
          onClick={handleDisconnect}
          disabled={loading}
          className={cn(
            'text-xs gap-1.5 transition-all',
            confirming
              ? 'text-red-400 bg-red-500/10 hover:bg-red-500/20'
              : 'text-zinc-500 hover:text-red-400 hover:bg-red-500/10'
          )}
        >
          <Unlink className="w-3 h-3" />
          {loading ? 'Desvinculando...' : confirming ? '¿Confirmar?' : 'Desvincular'}
        </Button>
      )}

      {!identity.isActive && (
        <Button variant="ghost" size="sm" className="text-xs gap-1.5 text-zinc-500 hover:text-violet-400">
          <ExternalLink className="w-3 h-3" />
          Conectar
        </Button>
      )}
    </motion.div>
  )
}

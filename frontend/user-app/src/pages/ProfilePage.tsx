import { motion } from 'framer-motion'
import { Loader2, Mail, Coins, Clock } from 'lucide-react'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { useCurrentUser } from '@/hooks/useCurrentUser'
import { useMyIdentities } from '@/hooks/useMyIdentities'
import { IdentityCard } from '@/components/identity/IdentityCard'
import { queryClient } from '@/lib/queryClient'

export default function ProfilePage() {
  const { data: user, isLoading: userLoading } = useCurrentUser()
  const { data: identities, isLoading: identitiesLoading, refetch } = useMyIdentities()

  const activeCount = identities?.filter((i) => i.isActive).length ?? 0

  const initials = user?.displayName
    ? user.displayName.slice(0, 2).toUpperCase()
    : user?.email?.slice(0, 2).toUpperCase() ?? 'VX'

  if (userLoading) {
    return (
      <div className="flex justify-center py-20">
        <Loader2 className="w-8 h-8 text-violet-400 animate-spin" />
      </div>
    )
  }

  return (
    <motion.div initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} className="max-w-2xl mx-auto space-y-8">
      {/* Profile header */}
      <div className="bg-gradient-to-br from-violet-600/10 via-transparent to-blue-600/10 border border-white/10 rounded-3xl p-8 flex items-start gap-6">
        <Avatar className="w-20 h-20 ring-4 ring-violet-500/30 flex-shrink-0">
          <AvatarImage src={user?.avatarUrl} />
          <AvatarFallback className="bg-violet-600 text-white text-2xl font-black">
            {initials}
          </AvatarFallback>
        </Avatar>

        <div className="flex-1 min-w-0">
          <h1 className="text-2xl font-black text-white">
            {user?.displayName ?? 'Usuario'}
          </h1>
          <div className="flex items-center gap-2 mt-1">
            <Mail className="w-4 h-4 text-zinc-500 flex-shrink-0" />
            <p className="text-zinc-400 text-sm truncate">{user?.email}</p>
          </div>

          <div className="flex gap-4 mt-5">
            <div className="text-center">
              <div className="flex items-center gap-1.5 text-violet-400">
                <Coins className="w-4 h-4" />
                <span className="text-xl font-black text-white">{user?.totalPoints.toLocaleString()}</span>
              </div>
              <p className="text-xs text-zinc-600 mt-0.5">Puntos totales</p>
            </div>
            <div className="text-center">
              <div className="flex items-center gap-1.5 text-zinc-400">
                <Clock className="w-4 h-4" />
                <span className="text-xl font-black text-white">
                  {user?.createdAt ? new Date(user.createdAt).getFullYear() : '—'}
                </span>
              </div>
              <p className="text-xs text-zinc-600 mt-0.5">Miembro desde</p>
            </div>
          </div>
        </div>
      </div>

      {/* Identities section */}
      <section>
        <div className="flex items-center justify-between mb-4">
          <div>
            <h2 className="text-lg font-bold text-white">Identidades vinculadas</h2>
            <p className="text-sm text-zinc-500 mt-0.5">
              {activeCount} {activeCount === 1 ? 'método activo' : 'métodos activos'}
            </p>
          </div>
        </div>

        {identitiesLoading ? (
          <div className="space-y-3">
            {Array.from({ length: 2 }).map((_, i) => (
              <div key={i} className="h-16 rounded-xl bg-white/5 animate-pulse" />
            ))}
          </div>
        ) : (
          <div className="space-y-3">
            {identities?.map((identity) => (
              <IdentityCard
                key={identity.id}
                identity={identity}
                canDisconnect={activeCount > 1}
                onDisconnected={() => {
                  refetch()
                  queryClient.invalidateQueries({ queryKey: ['me'] })
                }}
              />
            ))}

            {(!identities || identities.length === 0) && (
              <div className="text-center py-8 text-zinc-600">
                <p>No hay identidades vinculadas</p>
              </div>
            )}
          </div>
        )}
      </section>
    </motion.div>
  )
}

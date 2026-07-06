import { useQuery } from '@tanstack/react-query'
import { Gift, Store, TrendingUp, Package, Loader2, AlertCircle, CheckCircle2 } from 'lucide-react'
import { api } from '@/lib/api'
import type { GiftCardProvider } from '@/types/api'
import { cn } from '@/lib/utils'

function StatCard({
  label,
  value,
  sub,
  icon: Icon,
  accent,
}: {
  label: string
  value: string | number
  sub?: string
  icon: React.ElementType
  accent: string
}) {
  return (
    <div className="bg-zinc-900 border border-zinc-800 rounded-xl p-5 flex flex-col gap-3">
      <div className="flex items-center justify-between">
        <p className="text-sm font-medium text-zinc-400">{label}</p>
        <div className={cn('w-9 h-9 rounded-lg flex items-center justify-center', accent)}>
          <Icon className="w-4 h-4" />
        </div>
      </div>
      <div>
        <p className="text-3xl font-bold text-zinc-100">{value}</p>
        {sub && <p className="text-xs text-zinc-500 mt-0.5">{sub}</p>}
      </div>
    </div>
  )
}

function StockBadge({ pct }: { pct: number }) {
  if (pct > 50) return <span className="text-xs font-semibold text-emerald-400 bg-emerald-500/10 border border-emerald-500/20 px-2.5 py-0.5 rounded-full">{pct.toFixed(1)}%</span>
  if (pct > 20) return <span className="text-xs font-semibold text-amber-400 bg-amber-500/10 border border-amber-500/20 px-2.5 py-0.5 rounded-full">{pct.toFixed(1)}%</span>
  return <span className="text-xs font-semibold text-red-400 bg-red-500/10 border border-red-500/20 px-2.5 py-0.5 rounded-full">{pct.toFixed(1)}%</span>
}

export default function DashboardPage() {
  const { data: providers, isLoading, isError } = useQuery<GiftCardProvider[]>({
    queryKey: ['providers'],
    queryFn: async () => {
      const res = await api.get('/admin/giftcard/providers')
      return res.data
    },
  })

  const activeProviders = providers?.filter((p) => p.isActive) ?? []
  const totalCodes = providers?.reduce((sum, p) => sum + (p.stockCount ?? 0), 0) ?? 0
  const availableCodes = providers?.reduce((sum, p) => sum + (p.availableCount ?? 0), 0) ?? 0
  const usedCodes = totalCodes - availableCodes
  const availablePct = totalCodes > 0 ? (availableCodes / totalCodes) * 100 : 0

  if (isLoading) {
    return (
      <div className="p-8 flex items-center gap-3 text-zinc-400">
        <Loader2 className="w-5 h-5 animate-spin" />
        Cargando datos...
      </div>
    )
  }

  if (isError) {
    return (
      <div className="p-8">
        <div className="flex items-center gap-3 bg-red-500/10 border border-red-500/20 rounded-xl px-5 py-4 text-red-400">
          <AlertCircle className="w-5 h-5 flex-shrink-0" />
          <p className="text-sm">Error al obtener datos de providers. Verifica la conexión con el backend.</p>
        </div>
      </div>
    )
  }

  return (
    <div className="p-8 space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-zinc-100">Dashboard</h1>
        <p className="text-sm text-zinc-500 mt-1">Resumen general del sistema ValiantXP</p>
      </div>

      {/* Stats grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
        <StatCard
          label="Providers activos"
          value={activeProviders.length}
          sub={`de ${providers?.length ?? 0} totales`}
          icon={Store}
          accent="bg-violet-600/20 text-violet-400"
        />
        <StatCard
          label="Códigos totales"
          value={totalCodes.toLocaleString()}
          sub="en todos los providers"
          icon={Package}
          accent="bg-blue-600/20 text-blue-400"
        />
        <StatCard
          label="Disponibles"
          value={availableCodes.toLocaleString()}
          sub={`${availablePct.toFixed(1)}% del total`}
          icon={Gift}
          accent="bg-emerald-600/20 text-emerald-400"
        />
        <StatCard
          label="Utilizados"
          value={usedCodes.toLocaleString()}
          sub="canjeados por usuarios"
          icon={TrendingUp}
          accent="bg-amber-600/20 text-amber-400"
        />
      </div>

      {/* Providers table */}
      <div className="bg-zinc-900 border border-zinc-800 rounded-xl overflow-hidden">
        <div className="px-6 py-4 border-b border-zinc-800 flex items-center justify-between">
          <h2 className="text-sm font-semibold text-zinc-100">GiftCard Providers</h2>
          <span className="text-xs text-zinc-500">{providers?.length ?? 0} providers registrados</span>
        </div>

        <div className="divide-y divide-zinc-800">
          {providers?.length === 0 && (
            <div className="px-6 py-10 text-center text-sm text-zinc-500">No hay providers registrados.</div>
          )}
          {providers?.map((provider) => {
            const pct = (provider.stockCount ?? 0) > 0
              ? ((provider.availableCount ?? 0) / (provider.stockCount ?? 1)) * 100
              : 0
            return (
              <div key={provider.id} className="px-6 py-4 flex items-center gap-4 hover:bg-zinc-800/40 transition-colors">
                {/* Logo / initials */}
                <div className="w-9 h-9 rounded-lg bg-zinc-800 flex items-center justify-center flex-shrink-0 overflow-hidden">
                  {provider.logoUrl ? (
                    <img src={provider.logoUrl} alt={provider.name} className="w-full h-full object-cover" />
                  ) : (
                    <span className="text-sm font-bold text-zinc-300">{provider.name.charAt(0)}</span>
                  )}
                </div>

                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold text-zinc-100">{provider.name}</p>
                  {provider.instructiveUrl && (
                    <a
                      href={provider.instructiveUrl}
                      target="_blank"
                      rel="noreferrer"
                      className="text-xs text-zinc-500 hover:text-violet-400 truncate block transition-colors"
                    >
                      {provider.instructiveUrl}
                    </a>
                  )}
                </div>

                {/* Status */}
                <div className="flex items-center gap-1.5">
                  {provider.isActive ? (
                    <>
                      <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400" />
                      <span className="text-xs text-emerald-400">Activo</span>
                    </>
                  ) : (
                    <>
                      <div className="w-3.5 h-3.5 rounded-full border-2 border-zinc-600" />
                      <span className="text-xs text-zinc-500">Inactivo</span>
                    </>
                  )}
                </div>

                {/* Stock */}
                <div className="flex items-center gap-4 ml-4">
                  <div className="text-right">
                    <p className="text-xs text-zinc-500">Stock</p>
                    <p className="text-sm font-semibold text-zinc-200">
                      {(provider.availableCount ?? 0).toLocaleString()} / {(provider.stockCount ?? 0).toLocaleString()}
                    </p>
                  </div>
                  <StockBadge pct={pct} />
                </div>
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}

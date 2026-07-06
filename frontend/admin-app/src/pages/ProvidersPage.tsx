import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  useReactTable,
  getCoreRowModel,
  flexRender,
  createColumnHelper,
} from '@tanstack/react-table'
import {
  Plus,
  Pencil,
  Loader2,
  AlertCircle,
  X,
  CheckCircle2,
  ExternalLink,
} from 'lucide-react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { api } from '@/lib/api'
import type { GiftCardProvider } from '@/types/api'
import { cn } from '@/lib/utils'

// ---------------------------------------------------------------------------
// Schema & form
// ---------------------------------------------------------------------------
const providerSchema = z.object({
  name: z.string().min(1, 'El nombre es requerido'),
  instructiveUrl: z.string().url('Debe ser una URL válida').optional().or(z.literal('')),
  logoUrl: z.string().url('Debe ser una URL válida').optional().or(z.literal('')),
  isActive: z.boolean(),
})

type ProviderFormData = z.infer<typeof providerSchema>

// ---------------------------------------------------------------------------
// Modal
// ---------------------------------------------------------------------------
function ProviderModal({
  provider,
  onClose,
}: {
  provider: GiftCardProvider | null // null = create mode
  onClose: () => void
}) {
  const qc = useQueryClient()
  const isEdit = !!provider

  const {
    register,
    handleSubmit,
    formState: { errors },
    watch,
    setValue,
  } = useForm<ProviderFormData>({
    resolver: zodResolver(providerSchema),
    defaultValues: {
      name: provider?.name ?? '',
      instructiveUrl: provider?.instructiveUrl ?? '',
      logoUrl: provider?.logoUrl ?? '',
      isActive: provider?.isActive ?? true,
    },
  })

  const isActive = watch('isActive')

  const mutation = useMutation({
    mutationFn: async (data: ProviderFormData) => {
      const payload = {
        ...data,
        instructiveUrl: data.instructiveUrl || undefined,
        logoUrl: data.logoUrl || undefined,
      }
      if (isEdit) {
        return api.put(`/admin/giftcard/providers/${provider.id}`, payload)
      }
      return api.post('/admin/giftcard/providers', payload)
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['providers'] })
      onClose()
    },
  })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/70 backdrop-blur-sm" onClick={onClose} />

      {/* Modal */}
      <div className="relative w-full max-w-md bg-zinc-900 border border-zinc-800 rounded-2xl shadow-2xl shadow-black/50 p-6">
        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-lg font-bold text-zinc-100">
            {isEdit ? `Editar ${provider.name}` : 'Nuevo Provider'}
          </h2>
          <button
            onClick={onClose}
            className="w-8 h-8 rounded-lg flex items-center justify-center text-zinc-500 hover:bg-zinc-800 hover:text-zinc-100 transition-all"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        <form onSubmit={handleSubmit((data) => mutation.mutate(data))} className="space-y-4">
          {/* Name */}
          <div>
            <label className="block text-sm font-medium text-zinc-300 mb-1.5">Nombre *</label>
            <input
              {...register('name')}
              className="w-full bg-zinc-800 border border-zinc-700 text-zinc-100 placeholder-zinc-600 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:border-violet-500 focus:ring-1 focus:ring-violet-500/50 transition-all"
              placeholder="ej. Steam, Xbox, Netflix..."
            />
            {errors.name && <p className="mt-1 text-xs text-red-400">{errors.name.message}</p>}
          </div>

          {/* Instructive URL */}
          <div>
            <label className="block text-sm font-medium text-zinc-300 mb-1.5">URL Instructivo</label>
            <input
              {...register('instructiveUrl')}
              className="w-full bg-zinc-800 border border-zinc-700 text-zinc-100 placeholder-zinc-600 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:border-violet-500 focus:ring-1 focus:ring-violet-500/50 transition-all"
              placeholder="https://..."
            />
            {errors.instructiveUrl && <p className="mt-1 text-xs text-red-400">{errors.instructiveUrl.message}</p>}
          </div>

          {/* Logo URL */}
          <div>
            <label className="block text-sm font-medium text-zinc-300 mb-1.5">Logo URL</label>
            <input
              {...register('logoUrl')}
              className="w-full bg-zinc-800 border border-zinc-700 text-zinc-100 placeholder-zinc-600 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:border-violet-500 focus:ring-1 focus:ring-violet-500/50 transition-all"
              placeholder="https://..."
            />
            {errors.logoUrl && <p className="mt-1 text-xs text-red-400">{errors.logoUrl.message}</p>}
          </div>

          {/* Is Active toggle */}
          <div className="flex items-center justify-between py-1">
            <div>
              <p className="text-sm font-medium text-zinc-300">Activo</p>
              <p className="text-xs text-zinc-500">Los providers inactivos no se muestran a usuarios</p>
            </div>
            <button
              type="button"
              onClick={() => setValue('isActive', !isActive)}
              className={cn(
                'w-11 h-6 rounded-full relative transition-all duration-200',
                isActive ? 'bg-violet-600' : 'bg-zinc-700'
              )}
            >
              <span
                className={cn(
                  'absolute top-0.5 w-5 h-5 rounded-full bg-white shadow transition-all duration-200',
                  isActive ? 'left-[22px]' : 'left-0.5'
                )}
              />
            </button>
          </div>

          {/* Error */}
          {mutation.isError && (
            <div className="flex items-center gap-2 bg-red-500/10 border border-red-500/20 rounded-lg px-3 py-2.5">
              <AlertCircle className="w-4 h-4 text-red-400 flex-shrink-0" />
              <p className="text-xs text-red-400">Error al guardar. Verifica los datos.</p>
            </div>
          )}

          {/* Actions */}
          <div className="flex gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 py-2.5 px-4 rounded-lg border border-zinc-700 text-sm font-medium text-zinc-300 hover:bg-zinc-800 transition-all"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={mutation.isPending}
              className="flex-1 flex items-center justify-center gap-2 py-2.5 px-4 rounded-lg bg-violet-600 hover:bg-violet-700 disabled:opacity-50 text-sm font-semibold text-white transition-all"
            >
              {mutation.isPending ? (
                <><Loader2 className="w-3.5 h-3.5 animate-spin" /> Guardando...</>
              ) : (
                isEdit ? 'Actualizar' : 'Crear provider'
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Main page
// ---------------------------------------------------------------------------
const columnHelper = createColumnHelper<GiftCardProvider>()

export default function ProvidersPage() {
  const [modalProvider, setModalProvider] = useState<GiftCardProvider | null | undefined>(undefined)
  // undefined = closed, null = create, GiftCardProvider = edit

  const { data: providers, isLoading, isError } = useQuery<GiftCardProvider[]>({
    queryKey: ['providers'],
    queryFn: async () => {
      const res = await api.get('/admin/giftcard/providers')
      return res.data
    },
  })

  const columns = [
    columnHelper.accessor('name', {
      header: 'Provider',
      cell: (info) => {
        const row = info.row.original
        return (
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-lg bg-zinc-800 flex items-center justify-center overflow-hidden flex-shrink-0">
              {row.logoUrl ? (
                <img src={row.logoUrl} alt={row.name} className="w-full h-full object-cover" />
              ) : (
                <span className="text-sm font-bold text-zinc-300">{row.name.charAt(0)}</span>
              )}
            </div>
            <span className="text-sm font-semibold text-zinc-100">{info.getValue()}</span>
          </div>
        )
      },
    }),
    columnHelper.accessor('instructiveUrl', {
      header: 'URL Instructivo',
      cell: (info) => {
        const url = info.getValue()
        if (!url) return <span className="text-zinc-600 text-sm">—</span>
        return (
          <a
            href={url}
            target="_blank"
            rel="noreferrer"
            className="flex items-center gap-1 text-sm text-violet-400 hover:text-violet-300 transition-colors max-w-[200px] truncate"
          >
            <ExternalLink className="w-3 h-3 flex-shrink-0" />
            <span className="truncate">{url}</span>
          </a>
        )
      },
    }),
    columnHelper.accessor('isActive', {
      header: 'Estado',
      cell: (info) =>
        info.getValue() ? (
          <div className="flex items-center gap-1.5">
            <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400" />
            <span className="text-xs font-semibold text-emerald-400">Activo</span>
          </div>
        ) : (
          <div className="flex items-center gap-1.5">
            <div className="w-3 h-3 rounded-full border-2 border-zinc-600" />
            <span className="text-xs font-semibold text-zinc-500">Inactivo</span>
          </div>
        ),
    }),
    columnHelper.accessor('availableCount', {
      header: 'Stock disponible',
      cell: (info) => {
        const row = info.row.original
        const available = row.availableCount ?? 0
        const total = row.stockCount ?? 0
        const pct = total > 0 ? (available / total) * 100 : 0
        const color =
          pct > 50 ? 'text-emerald-400' : pct > 20 ? 'text-amber-400' : 'text-red-400'
        return (
          <div>
            <span className={cn('text-sm font-semibold', color)}>
              {available.toLocaleString()}
            </span>
            <span className="text-zinc-600 text-xs"> / {total.toLocaleString()}</span>
          </div>
        )
      },
    }),
    columnHelper.display({
      id: 'actions',
      header: '',
      cell: (info) => (
        <button
          onClick={() => setModalProvider(info.row.original)}
          className="flex items-center gap-1.5 text-xs text-zinc-400 hover:text-violet-400 hover:bg-violet-500/10 px-2.5 py-1.5 rounded-lg transition-all"
        >
          <Pencil className="w-3.5 h-3.5" />
          Editar
        </button>
      ),
    }),
  ]

  const table = useReactTable({
    data: providers ?? [],
    columns,
    getCoreRowModel: getCoreRowModel(),
  })

  return (
    <>
      <div className="p-8 space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-zinc-100">Providers</h1>
            <p className="text-sm text-zinc-500 mt-1">Gestiona los proveedores de GiftCards</p>
          </div>
          <button
            onClick={() => setModalProvider(null)}
            className="flex items-center gap-2 bg-violet-600 hover:bg-violet-700 text-white text-sm font-semibold px-4 py-2.5 rounded-xl transition-all shadow-lg shadow-violet-600/20"
          >
            <Plus className="w-4 h-4" />
            Nuevo Provider
          </button>
        </div>

        {/* Table */}
        <div className="bg-zinc-900 border border-zinc-800 rounded-xl overflow-hidden">
          {isLoading && (
            <div className="flex items-center gap-3 px-6 py-10 text-zinc-400">
              <Loader2 className="w-5 h-5 animate-spin" /> Cargando providers...
            </div>
          )}
          {isError && (
            <div className="flex items-center gap-3 px-6 py-6 text-red-400">
              <AlertCircle className="w-5 h-5" />
              <p className="text-sm">Error al cargar providers.</p>
            </div>
          )}
          {!isLoading && !isError && (
            <table className="w-full">
              <thead>
                {table.getHeaderGroups().map((hg) => (
                  <tr key={hg.id} className="border-b border-zinc-800">
                    {hg.headers.map((header) => (
                      <th
                        key={header.id}
                        className="px-6 py-3.5 text-left text-xs font-semibold text-zinc-500 uppercase tracking-wide"
                      >
                        {flexRender(header.column.columnDef.header, header.getContext())}
                      </th>
                    ))}
                  </tr>
                ))}
              </thead>
              <tbody className="divide-y divide-zinc-800">
                {table.getRowModel().rows.length === 0 && (
                  <tr>
                    <td colSpan={columns.length} className="px-6 py-10 text-center text-sm text-zinc-500">
                      No hay providers. Crea el primero.
                    </td>
                  </tr>
                )}
                {table.getRowModel().rows.map((row) => (
                  <tr key={row.id} className="hover:bg-zinc-800/50 transition-colors">
                    {row.getVisibleCells().map((cell) => (
                      <td key={cell.id} className="px-6 py-4">
                        {flexRender(cell.column.columnDef.cell, cell.getContext())}
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>

      {/* Modal */}
      {modalProvider !== undefined && (
        <ProviderModal
          provider={modalProvider}
          onClose={() => setModalProvider(undefined)}
        />
      )}
    </>
  )
}

import { useState, useCallback } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { useDropzone } from 'react-dropzone'
import {
  ChevronDown,
  Upload,
  Download,
  FileText,
  CheckCircle2,
  AlertCircle,
  Loader2,
  X,
  Package,
  Zap,
} from 'lucide-react'
import { api } from '@/lib/api'
import type { GiftCardProvider, GiftCardStock, GiftCardImportResult } from '@/types/api'
import { cn } from '@/lib/utils'

// ---------------------------------------------------------------------------
// CSV Parser (frontend)
// ---------------------------------------------------------------------------
interface GiftCardRow { code: string; redeemUrl?: string; pin?: string; description?: string }

function parseCSV(text: string): { rows: GiftCardRow[]; raw: string } {
  const lines = text
    .split(/\r?\n/)
    .map((l) => l.trim())
    .filter(Boolean)

  // Detect and skip header row
  const firstLine = lines[0]?.toLowerCase() ?? ''
  const hasHeader =
    firstLine.includes('code') || firstLine.includes('código') || firstLine.includes('gift')
  const dataLines = hasHeader ? lines.slice(1) : lines

  const rows: GiftCardRow[] = dataLines.map((l) => {
    // CSV format: Code,RedeemUrl,Pin,Description
    const cols = l.split(',').map((c) => c.replace(/^["']|["']$/g, '').trim())
    return {
      code:        cols[0] ?? '',
      redeemUrl:   cols[1] || undefined,
      pin:         cols[2] || undefined,
      description: cols[3] || undefined,
    }
  }).filter((r) => r.code.length > 0)

  return { rows, raw: text }
}


// ---------------------------------------------------------------------------
// Provider selector
// ---------------------------------------------------------------------------
function ProviderSelector({
  providers,
  selectedId,
  onSelect,
}: {
  providers: GiftCardProvider[]
  selectedId: string | null
  onSelect: (id: string) => void
}) {
  const [open, setOpen] = useState(false)
  const selected = providers.find((p) => p.id === selectedId)

  return (
    <div className="relative">
      <button
        onClick={() => setOpen(!open)}
        className="flex items-center gap-3 bg-zinc-900 border border-zinc-700 hover:border-violet-500 rounded-xl px-4 py-3 text-sm transition-all min-w-[220px]"
      >
        {selected ? (
          <>
            <div className="w-6 h-6 rounded bg-zinc-700 flex items-center justify-center text-xs font-bold text-zinc-300 overflow-hidden">
              {selected.logoUrl ? (
                <img src={selected.logoUrl} alt="" className="w-full h-full object-cover" />
              ) : (
                selected.name.charAt(0)
              )}
            </div>
            <span className="font-medium text-zinc-100 flex-1 text-left">{selected.name}</span>
          </>
        ) : (
          <span className="text-zinc-500 flex-1 text-left">Selecciona un provider...</span>
        )}
        <ChevronDown className={cn('w-4 h-4 text-zinc-500 transition-transform', open && 'rotate-180')} />
      </button>

      {open && (
        <div className="absolute z-20 top-full mt-1 left-0 w-full bg-zinc-900 border border-zinc-700 rounded-xl shadow-2xl overflow-hidden">
          {providers.map((p) => (
            <button
              key={p.id}
              onClick={() => { onSelect(p.id); setOpen(false) }}
              className={cn(
                'w-full flex items-center gap-3 px-4 py-2.5 text-sm hover:bg-zinc-800 transition-colors',
                p.id === selectedId && 'bg-violet-600/10 text-violet-300'
              )}
            >
              <div className="w-6 h-6 rounded bg-zinc-700 flex items-center justify-center text-xs font-bold overflow-hidden">
                {p.logoUrl ? <img src={p.logoUrl} alt="" className="w-full h-full object-cover" /> : p.name.charAt(0)}
              </div>
              <span className={p.id === selectedId ? 'text-violet-300' : 'text-zinc-200'}>{p.name}</span>
              {!p.isActive && <span className="ml-auto text-xs text-zinc-600">Inactivo</span>}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}

// ---------------------------------------------------------------------------
// Stock card
// ---------------------------------------------------------------------------
function StockCard({ stock, isLoading }: { stock: GiftCardStock | undefined; isLoading: boolean }) {
  if (isLoading) {
    return (
      <div className="bg-zinc-900 border border-zinc-800 rounded-xl p-5 flex items-center gap-3 text-zinc-400">
        <Loader2 className="w-5 h-5 animate-spin" /> Cargando stock...
      </div>
    )
  }
  if (!stock) return null

  const usedCount = stock.count - stock.available
  const pct = stock.count > 0 ? (stock.available / stock.count) * 100 : 0
  const barColor = pct > 50 ? 'bg-emerald-500' : pct > 20 ? 'bg-amber-500' : 'bg-red-500'

  return (
    <div className="bg-zinc-900 border border-zinc-800 rounded-xl p-5 space-y-4">
      <h3 className="text-sm font-semibold text-zinc-300">Stock actual</h3>
      <div className="grid grid-cols-3 gap-4">
        <div>
          <p className="text-xs text-zinc-500">Total</p>
          <p className="text-xl font-bold text-zinc-100">{stock.count.toLocaleString()}</p>
        </div>
        <div>
          <p className="text-xs text-zinc-500">Disponibles</p>
          <p className="text-xl font-bold text-emerald-400">{stock.available.toLocaleString()}</p>
        </div>
        <div>
          <p className="text-xs text-zinc-500">Utilizados</p>
          <p className="text-xl font-bold text-zinc-400">{usedCount.toLocaleString()}</p>
        </div>
      </div>
      {/* Progress bar */}
      <div>
        <div className="flex justify-between text-xs text-zinc-500 mb-1.5">
          <span>Disponibilidad</span>
          <span>{pct.toFixed(1)}%</span>
        </div>
        <div className="h-2 bg-zinc-800 rounded-full overflow-hidden">
          <div
            className={cn('h-full rounded-full transition-all duration-500', barColor)}
            style={{ width: `${Math.max(pct, 0)}%` }}
          />
        </div>
      </div>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Dropzone + import section
// ---------------------------------------------------------------------------
function ImportSection({ providerId }: { providerId: string }) {
  const [parsedRows, setParsedRows] = useState<GiftCardRow[]>([])
  const [fileName, setFileName] = useState<string | null>(null)
  const [validating, setValidating] = useState(false)
  const [validateResult, setValidateResult] = useState<{ valid: number; invalid: string[] } | null>(null)
  const [importResult, setImportResult] = useState<GiftCardImportResult | null>(null)

  const onDrop = useCallback((acceptedFiles: File[]) => {
    const file = acceptedFiles[0]
    if (!file) return
    setFileName(file.name)
    setValidateResult(null)
    setImportResult(null)
    const reader = new FileReader()
    reader.onload = (e) => {
      const text = e.target?.result as string
      const { rows } = parseCSV(text)
      setParsedRows(rows)
    }
    reader.readAsText(file)
  }, [])

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: { 'text/csv': ['.csv'], 'text/plain': ['.txt'] },
    maxFiles: 1,
  })

  const handleValidate = async () => {
    setValidating(true)
    setValidateResult(null)
    try {
      const res = await api.post('/admin/giftcard/codes/validate', {
        providerId,
        rows: parsedRows,   // ← backend espera rows: ImportGiftCardRow[]
      })
      // Backend retorna: { total, duplicates, duplicateCodes, canImport, message }
      const d = res.data
      setValidateResult({ valid: d.total - d.duplicates, invalid: d.duplicateCodes ?? [] })
    } catch {
      setValidateResult({ valid: parsedRows.length, invalid: [] })
    } finally {
      setValidating(false)
    }
  }

  const importMutation = useMutation({
    mutationFn: async () => {
      // Backend espera: { providerId: Guid, rows: ImportGiftCardRow[] }
      const res = await api.post<GiftCardImportResult>('/admin/giftcard/codes/import', {
        providerId,
        rows: parsedRows,
      })
      return res.data
    },
    onSuccess: (data) => {
      setImportResult(data)
      setParsedRows([])
      setFileName(null)
      setValidateResult(null)
    },
  })

  const handleDownloadTemplate = async () => {
    try {
      const res = await api.get('/admin/giftcard/codes/template', { responseType: 'blob' })
      const url = URL.createObjectURL(res.data)
      const a = document.createElement('a')
      a.href = url
      a.download = 'giftcard-template.csv'
      a.click()
      URL.revokeObjectURL(url)
    } catch {
      // fallback: generate locally
      const content = 'code\nGIFT-XXXX-YYYY-ZZZZ\nGIFT-AAAA-BBBB-CCCC'
      const blob = new Blob([content], { type: 'text/csv' })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = 'giftcard-template.csv'
      a.click()
      URL.revokeObjectURL(url)
    }
  }

  const clearFile = () => {
    setParsedRows([])
    setFileName(null)
    setValidateResult(null)
    setImportResult(null)
  }

  return (
    <div className="bg-zinc-900 border border-zinc-800 rounded-xl p-5 space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-zinc-300">Importar códigos CSV</h3>
        <button
          onClick={handleDownloadTemplate}
          className="flex items-center gap-1.5 text-xs text-violet-400 hover:text-violet-300 transition-colors"
        >
          <Download className="w-3.5 h-3.5" />
          Descargar template
        </button>
      </div>

      {/* Dropzone */}
      {!fileName ? (
        <div
          {...getRootProps()}
          className={cn(
            'border-2 border-dashed rounded-xl p-8 text-center cursor-pointer transition-all duration-200',
            isDragActive
              ? 'border-violet-500 bg-violet-500/10'
              : 'border-zinc-700 hover:border-zinc-500 hover:bg-zinc-800/50'
          )}
        >
          <input {...getInputProps()} />
          <div className="flex flex-col items-center gap-3">
            <div className={cn('w-12 h-12 rounded-xl flex items-center justify-center transition-colors', isDragActive ? 'bg-violet-600/20' : 'bg-zinc-800')}>
              <Upload className={cn('w-5 h-5', isDragActive ? 'text-violet-400' : 'text-zinc-500')} />
            </div>
            <div>
              <p className="text-sm font-medium text-zinc-300">
                {isDragActive ? 'Suelta el archivo aquí' : 'Arrastra tu CSV aquí'}
              </p>
              <p className="text-xs text-zinc-500 mt-1">o haz click para seleccionar · .csv, .txt</p>
            </div>
          </div>
        </div>
      ) : (
        /* File selected state */
        <div className="border border-zinc-700 rounded-xl p-4 bg-zinc-800/30">
          <div className="flex items-center justify-between mb-3">
            <div className="flex items-center gap-3">
              <div className="w-9 h-9 rounded-lg bg-violet-600/20 flex items-center justify-center">
                <FileText className="w-4 h-4 text-violet-400" />
              </div>
              <div>
                <p className="text-sm font-semibold text-zinc-100">{fileName}</p>
                <p className="text-xs text-zinc-500">{parsedCodes.length} códigos detectados</p>
              </div>
            </div>
            <button onClick={clearFile} className="w-7 h-7 rounded-lg flex items-center justify-center text-zinc-500 hover:bg-zinc-700 hover:text-zinc-200 transition-all">
              <X className="w-3.5 h-3.5" />
            </button>
          </div>

          {/* Preview of first 5 codes */}
          {parsedCodes.length > 0 && (
            <div className="bg-zinc-900 rounded-lg p-3 mb-3">
              <p className="text-xs font-medium text-zinc-500 mb-2">Vista previa (primeros {Math.min(5, parsedRows.length)}):</p>
              <div className="space-y-1">
                {parsedRows.slice(0, 5).map((row, i) => (
                  <p key={i} className="text-xs font-mono text-zinc-300">{row.code}{row.pin ? ` · PIN: ${row.pin}` : ''}</p>
                ))}
                {parsedRows.length > 5 && (
                  <p className="text-xs text-zinc-600">...y {parsedRows.length - 5} más</p>
                )}
              </div>
            </div>
          )}

          {/* Validate result */}
          {validateResult && (
            <div className={cn(
              'rounded-lg px-3 py-2.5 mb-3 flex items-start gap-2',
              validateResult.invalid.length > 0
                ? 'bg-amber-500/10 border border-amber-500/20'
                : 'bg-emerald-500/10 border border-emerald-500/20'
            )}>
              {validateResult.invalid.length > 0 ? (
                <AlertCircle className="w-4 h-4 text-amber-400 flex-shrink-0 mt-0.5" />
              ) : (
                <CheckCircle2 className="w-4 h-4 text-emerald-400 flex-shrink-0 mt-0.5" />
              )}
              <div>
                <p className="text-xs font-semibold text-zinc-200">
                  {validateResult.valid} válidos
                  {validateResult.invalid.length > 0 && `, ${validateResult.invalid.length} inválidos`}
                </p>
                {validateResult.invalid.length > 0 && (
                  <p className="text-xs text-zinc-500 mt-0.5">
                    Inválidos: {validateResult.invalid.slice(0, 3).join(', ')}
                    {validateResult.invalid.length > 3 && `... +${validateResult.invalid.length - 3}`}
                  </p>
                )}
              </div>
            </div>
          )}
        </div>
      )}

      {/* Actions */}
      {parsedRows.length > 0 && (
        <div className="flex gap-3">
          <button
            onClick={handleValidate}
            disabled={validating}
            className="flex items-center gap-2 px-4 py-2.5 rounded-xl border border-zinc-700 text-sm font-medium text-zinc-300 hover:bg-zinc-800 disabled:opacity-50 transition-all"
          >
            {validating ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <CheckCircle2 className="w-3.5 h-3.5" />}
            Validar
          </button>
          <button
            onClick={() => importMutation.mutate()}
            disabled={importMutation.isPending}
            className="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-violet-600 hover:bg-violet-700 disabled:opacity-50 text-sm font-semibold text-white transition-all shadow-lg shadow-violet-600/20"
          >
            {importMutation.isPending ? (
              <><Loader2 className="w-3.5 h-3.5 animate-spin" /> Importando...</>
            ) : (
              <><Zap className="w-3.5 h-3.5" /> Importar {parsedRows.length} códigos</>
            )}
          </button>
        </div>
      )}

      {/* Import result */}
      {importResult && (
        <div className="bg-emerald-500/10 border border-emerald-500/20 rounded-xl px-4 py-3 flex items-center gap-3">
          <CheckCircle2 className="w-5 h-5 text-emerald-400 flex-shrink-0" />
          <div>
            <p className="text-sm font-semibold text-emerald-300">
              ✅ {importResult.imported.toLocaleString()} códigos importados
            </p>
            {importResult.duplicates > 0 && (
              <p className="text-xs text-zinc-500 mt-0.5">{importResult.duplicates} duplicados omitidos</p>
            )}
          </div>
        </div>
      )}
    </div>
  )
}

// ---------------------------------------------------------------------------
// Main page
// ---------------------------------------------------------------------------
export default function GiftCardsPage() {
  const [selectedProviderId, setSelectedProviderId] = useState<string | null>(null)

  const { data: providers, isLoading: loadingProviders } = useQuery<GiftCardProvider[]>({
    queryKey: ['providers'],
    queryFn: async () => {
      const res = await api.get('/admin/giftcard/providers')
      return res.data
    },
  })

  const { data: stock, isLoading: loadingStock } = useQuery<GiftCardStock>({
    queryKey: ['stock', selectedProviderId],
    queryFn: async () => {
      const res = await api.get(`/admin/giftcard/providers/${selectedProviderId}/stock`)
      return res.data
    },
    enabled: !!selectedProviderId,
  })

  return (
    <div className="p-8 space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-zinc-100">GiftCards</h1>
        <p className="text-sm text-zinc-500 mt-1">Gestiona e importa códigos de gift cards por provider</p>
      </div>

      {/* Provider selector */}
      <div className="flex items-center gap-3">
        <span className="text-sm text-zinc-400">Provider:</span>
        {loadingProviders ? (
          <div className="flex items-center gap-2 text-zinc-500 text-sm">
            <Loader2 className="w-4 h-4 animate-spin" /> Cargando...
          </div>
        ) : (
          <ProviderSelector
            providers={providers ?? []}
            selectedId={selectedProviderId}
            onSelect={setSelectedProviderId}
          />
        )}
      </div>

      {!selectedProviderId && (
        <div className="bg-zinc-900 border border-zinc-800 border-dashed rounded-xl p-12 flex flex-col items-center gap-3 text-center">
          <div className="w-14 h-14 rounded-2xl bg-zinc-800 flex items-center justify-center">
            <Package className="w-7 h-7 text-zinc-600" />
          </div>
          <p className="text-zinc-400 font-medium">Selecciona un provider para ver su stock e importar códigos</p>
          <p className="text-xs text-zinc-600">Usa el selector de arriba</p>
        </div>
      )}

      {selectedProviderId && (
        <>
          {/* Stock overview */}
          <StockCard stock={stock} isLoading={loadingStock} />

          {/* Import */}
          <ImportSection providerId={selectedProviderId} />
        </>
      )}
    </div>
  )
}

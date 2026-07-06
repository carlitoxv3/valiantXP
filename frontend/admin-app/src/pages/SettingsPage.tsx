import { useState } from 'react'
import { api } from '@/lib/api'
import { CheckCircle2, XCircle, Loader2, ToggleLeft, ToggleRight } from 'lucide-react'

const APP_VERSION = '1.0.0'

const OAUTH_PROVIDERS = [
  { id: 'google', label: 'Google', desc: 'Login con cuenta Google' },
  { id: 'discord', label: 'Discord', desc: 'Login con cuenta Discord' },
  { id: 'twitch', label: 'Twitch', desc: 'Login con cuenta Twitch' },
  { id: 'steam', label: 'Steam', desc: 'Login con cuenta Steam' },
]

export default function SettingsPage() {
  const [oauthEnabled, setOauthEnabled] = useState<Record<string, boolean>>({
    google: true,
    discord: true,
    twitch: false,
    steam: false,
  })

  const [apiUrl, setApiUrl] = useState(import.meta.env.VITE_API_URL || 'http://localhost:5000')
  const [testStatus, setTestStatus] = useState<'idle' | 'loading' | 'ok' | 'error'>('idle')

  const handleTestConnection = async () => {
    setTestStatus('loading')
    try {
      await api.get('/users/me')
      setTestStatus('ok')
    } catch {
      setTestStatus('error')
    }
  }

  return (
    <div className="p-8 space-y-8 max-w-2xl">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-zinc-100">Configuración</h1>
        <p className="text-sm text-zinc-500 mt-1">Ajustes del panel de administración</p>
      </div>

      {/* OAuth Providers */}
      <section className="bg-zinc-900 border border-zinc-800 rounded-xl overflow-hidden">
        <div className="px-6 py-4 border-b border-zinc-800">
          <h2 className="text-sm font-semibold text-zinc-100">OAuth Providers</h2>
          <p className="text-xs text-zinc-500 mt-0.5">Controla qué métodos de login están disponibles (UI only)</p>
        </div>
        <div className="divide-y divide-zinc-800">
          {OAUTH_PROVIDERS.map((p) => (
            <div key={p.id} className="flex items-center justify-between px-6 py-4">
              <div>
                <p className="text-sm font-medium text-zinc-200">{p.label}</p>
                <p className="text-xs text-zinc-500">{p.desc}</p>
              </div>
              <button
                onClick={() => setOauthEnabled((prev) => ({ ...prev, [p.id]: !prev[p.id] }))}
                className="transition-colors"
              >
                {oauthEnabled[p.id] ? (
                  <ToggleRight className="w-8 h-8 text-violet-400" />
                ) : (
                  <ToggleLeft className="w-8 h-8 text-zinc-600" />
                )}
              </button>
            </div>
          ))}
        </div>
      </section>

      {/* API Connection */}
      <section className="bg-zinc-900 border border-zinc-800 rounded-xl overflow-hidden">
        <div className="px-6 py-4 border-b border-zinc-800">
          <h2 className="text-sm font-semibold text-zinc-100">API Connection</h2>
          <p className="text-xs text-zinc-500 mt-0.5">URL base del backend ValiantXP</p>
        </div>
        <div className="px-6 py-5 space-y-4">
          <div>
            <label className="block text-sm font-medium text-zinc-400 mb-1.5">VITE_API_URL</label>
            <div className="flex gap-3">
              <input
                type="text"
                value={apiUrl}
                onChange={(e) => { setApiUrl(e.target.value); setTestStatus('idle') }}
                className="flex-1 bg-zinc-800 border border-zinc-700 text-zinc-100 rounded-lg px-3 py-2.5 text-sm font-mono focus:outline-none focus:border-violet-500 focus:ring-1 focus:ring-violet-500/50 transition-all"
              />
              <button
                onClick={handleTestConnection}
                disabled={testStatus === 'loading'}
                className="flex items-center gap-2 px-4 py-2.5 rounded-lg bg-violet-600 hover:bg-violet-700 disabled:opacity-50 text-sm font-semibold text-white transition-all"
              >
                {testStatus === 'loading' ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  'Test'
                )}
              </button>
            </div>
            <p className="mt-1.5 text-xs text-zinc-600">
              Definido en .env · Para cambiar permanentemente, edita VITE_API_URL en el .env
            </p>
          </div>

          {/* Test result */}
          {testStatus === 'ok' && (
            <div className="flex items-center gap-2 bg-emerald-500/10 border border-emerald-500/20 rounded-lg px-3 py-2.5">
              <CheckCircle2 className="w-4 h-4 text-emerald-400 flex-shrink-0" />
              <p className="text-sm text-emerald-400">Conexión exitosa — backend responde correctamente</p>
            </div>
          )}
          {testStatus === 'error' && (
            <div className="flex items-center gap-2 bg-red-500/10 border border-red-500/20 rounded-lg px-3 py-2.5">
              <XCircle className="w-4 h-4 text-red-400 flex-shrink-0" />
              <p className="text-sm text-red-400">Sin conexión — verifica que el backend esté corriendo</p>
            </div>
          )}
        </div>
      </section>

      {/* About */}
      <section className="bg-zinc-900 border border-zinc-800 rounded-xl overflow-hidden">
        <div className="px-6 py-4 border-b border-zinc-800">
          <h2 className="text-sm font-semibold text-zinc-100">Acerca de</h2>
        </div>
        <div className="px-6 py-5 space-y-3">
          <div className="flex justify-between items-center">
            <span className="text-sm text-zinc-400">Versión</span>
            <span className="text-sm font-mono font-semibold text-zinc-200">v{APP_VERSION}</span>
          </div>
          <div className="flex justify-between items-center">
            <span className="text-sm text-zinc-400">Plataforma</span>
            <span className="text-sm text-zinc-200">ValiantXP Admin Panel</span>
          </div>
          <div className="flex justify-between items-center">
            <span className="text-sm text-zinc-400">Stack</span>
            <span className="text-sm text-zinc-400 font-mono">Vite · React · TypeScript · Tailwind</span>
          </div>
        </div>
      </section>
    </div>
  )
}

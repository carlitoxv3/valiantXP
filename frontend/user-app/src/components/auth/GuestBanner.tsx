import { useNavigate } from 'react-router-dom'
import { AlertTriangle, X } from 'lucide-react'
import { useAuthStore } from '@/stores/authStore'

export function GuestBanner() {
  const navigate = useNavigate()
  const setGuestToken = useAuthStore((s) => s.setGuestToken)

  const dismiss = () => setGuestToken('')

  return (
    <div className="flex items-center justify-between gap-4 px-4 py-2.5 bg-amber-500/10 border-b border-amber-500/20 text-amber-300 text-sm">
      <div className="flex items-center gap-2">
        <AlertTriangle className="w-4 h-4 flex-shrink-0" />
        <span>
          Participando como invitado · Tus premios se perderán si no te registras.
        </span>
      </div>
      <div className="flex items-center gap-3 flex-shrink-0">
        <button
          onClick={() => navigate('/login')}
          className="font-semibold text-amber-200 hover:text-white underline-offset-2 underline transition-colors"
        >
          → Registrarme
        </button>
        <button
          onClick={dismiss}
          className="hover:text-amber-100 transition-colors"
        >
          <X className="w-4 h-4" />
        </button>
      </div>
    </div>
  )
}

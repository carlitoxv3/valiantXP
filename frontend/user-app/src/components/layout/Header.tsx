import { useNavigate } from 'react-router-dom'
import { LogOut, Bell } from 'lucide-react'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import { useAuthStore } from '@/stores/authStore'

export function Header() {
  const navigate = useNavigate()
  const { user, clearAuth } = useAuthStore()

  const handleLogout = () => {
    clearAuth()
    navigate('/login')
  }

  const initials = user?.displayName
    ? user.displayName.slice(0, 2).toUpperCase()
    : user?.email?.slice(0, 2).toUpperCase() ?? 'VX'

  return (
    <header className="h-16 border-b border-white/5 bg-zinc-900/60 backdrop-blur-sm flex items-center justify-between px-6 flex-shrink-0">
      <div className="flex items-center gap-3">
        <div className="flex flex-col">
          <span className="text-sm font-semibold text-zinc-200">
            {user?.displayName ?? user?.email ?? 'Usuario'}
          </span>
          {user?.totalPoints !== undefined && (
            <span className="text-xs text-violet-400 font-medium">
              {user.totalPoints.toLocaleString()} pts
            </span>
          )}
        </div>
      </div>

      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" className="text-zinc-400 hover:text-zinc-200 hover:bg-white/5">
          <Bell className="w-4 h-4" />
        </Button>

        <Avatar className="w-8 h-8 ring-2 ring-violet-500/30">
          <AvatarImage src={user?.avatarUrl} alt={user?.displayName} />
          <AvatarFallback className="bg-violet-600 text-white text-xs font-bold">
            {initials}
          </AvatarFallback>
        </Avatar>

        <Button
          variant="ghost"
          size="icon"
          onClick={handleLogout}
          className="text-zinc-400 hover:text-red-400 hover:bg-red-500/10"
        >
          <LogOut className="w-4 h-4" />
        </Button>
      </div>
    </header>
  )
}

import { NavLink } from 'react-router-dom'
import { Home, Swords, Trophy, User, Zap, FlaskConical } from 'lucide-react'
import { cn } from '@/lib/utils'

const navItems = [
  { to: '/', icon: Home, label: 'Inicio', end: true },
  { to: '/challenges', icon: Swords, label: 'Desafíos' },
  { to: '/prizes', icon: Trophy, label: 'Premios' },
  { to: '/profile', icon: User, label: 'Perfil' },
  { to: '/demo', icon: FlaskConical, label: 'Demo' },
]

export function Sidebar() {
  return (
    <aside className="w-64 bg-zinc-900/80 border-r border-white/5 flex flex-col">
      {/* Logo */}
      <div className="p-6 border-b border-white/5">
        <div className="flex items-center gap-2">
          <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-violet-500 to-blue-500 flex items-center justify-center">
            <Zap className="w-4 h-4 text-white" />
          </div>
          <span className="font-bold text-white text-lg tracking-tight">
            Valiant<span className="text-violet-400">XP</span>
          </span>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 p-4 space-y-1">
        {navItems.map(({ to, icon: Icon, label, end }) => (
          <NavLink
            key={to}
            to={to}
            end={end}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-all duration-200',
                isActive
                  ? 'bg-violet-600/20 text-violet-300 border border-violet-500/30 shadow-lg shadow-violet-500/10'
                  : 'text-zinc-400 hover:text-zinc-200 hover:bg-white/5'
              )
            }
          >
            <Icon className="w-4 h-4 flex-shrink-0" />
            {label}
          </NavLink>
        ))}
      </nav>

      {/* Footer */}
      <div className="p-4 border-t border-white/5">
        <p className="text-xs text-zinc-600 text-center">ValiantXP © 2025</p>
      </div>
    </aside>
  )
}

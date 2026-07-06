import { useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { Brain, Mic, Code2, Trophy, CreditCard, Users, Zap, Star } from 'lucide-react'
import { TriviaChallenge } from '@/components/challenges/TriviaChallenge'
import { SurveyChallenge } from '@/components/challenges/SurveyChallenge'
import { CodeChallenge } from '@/components/challenges/CodeChallenge'
import { GiftCardReveal } from '@/components/prizes/GiftCardReveal'
import type { TriviaConfig, SurveyConfig } from '@/types/api'
import { cn } from '@/lib/utils'

// ── Demo Data ────────────────────────────────────────────
const DEMO_TRIVIA: TriviaConfig = {
  timeLimitSeconds: 15,
  questions: [
    {
      question: '¿Cuál es el framework de .NET para APIs REST?',
      options: [
        { text: 'ASP.NET Core', isCorrect: true },
        { text: 'Django', isCorrect: false },
        { text: 'Express', isCorrect: false },
        { text: 'Spring Boot', isCorrect: false },
      ],
    },
    {
      question: '¿Qué patrón usa ValiantXP para las estrategias?',
      options: [
        { text: 'Strategy Pattern', isCorrect: true },
        { text: 'Singleton', isCorrect: false },
        { text: 'Observer', isCorrect: false },
        { text: 'Factory', isCorrect: false },
      ],
    },
    {
      question: '¿Qué biblioteca maneja eventos de dominio en ValiantXP?',
      options: [
        { text: 'MediatR', isCorrect: true },
        { text: 'Hangfire', isCorrect: false },
        { text: 'SignalR', isCorrect: false },
        { text: 'RabbitMQ', isCorrect: false },
      ],
    },
  ],
}

const DEMO_SURVEY: SurveyConfig = {
  questions: [
    {
      id: 'q1',
      question: '¿Cómo calificarías tu experiencia con ValiantXP?',
      type: 'rating',
      required: true,
    },
    {
      id: 'q2',
      question: '¿Qué tipo de desafíos prefieres?',
      type: 'multiple_choice',
      options: ['Trivia de conocimiento', 'Encuestas de opinión', 'Códigos promocionales', 'Todos por igual'],
      required: true,
    },
    {
      id: 'q3',
      question: '¿Tienes algún comentario adicional?',
      type: 'text',
      required: false,
    },
  ],
}

// ── Tabs ────────────────────────────────────────────────
const tabs = [
  { id: 'trivia', label: 'Trivia', icon: Brain, color: 'text-blue-400', badge: 'TRIVIA', badgeColor: 'bg-blue-500/20 border-blue-500/30 text-blue-300', desc: 'Preguntas cronometradas con feedback en tiempo real' },
  { id: 'survey', label: 'Encuesta', icon: Mic, color: 'text-green-400', badge: 'SURVEY', badgeColor: 'bg-green-500/20 border-green-500/30 text-green-300', desc: 'Formularios multi-paso con distintos tipos de pregunta' },
  { id: 'code', label: 'Código', icon: Code2, color: 'text-amber-400', badge: 'CODE', badgeColor: 'bg-amber-500/20 border-amber-500/30 text-amber-300', desc: 'Validación de códigos únicos. Prueba: DEMO2024' },
  { id: 'prize', label: 'Premio', icon: Trophy, color: 'text-violet-400', badge: 'INSTANT WIN', badgeColor: 'bg-violet-500/20 border-violet-500/30 text-violet-300', desc: 'Animación de premio ganado con confetti' },
  { id: 'giftcard', label: 'Gift Card', icon: CreditCard, color: 'text-pink-400', badge: 'GIFT CARD', badgeColor: 'bg-pink-500/20 border-pink-500/30 text-pink-300', desc: 'Revelar código con efecto rasca y gana' },
  { id: 'guest', label: 'Invitado', icon: Users, color: 'text-amber-400', badge: 'GUEST', badgeColor: 'bg-amber-500/20 border-amber-500/30 text-amber-300', desc: 'Flujo de participación sin registro' },
]

// ── Animated gradient text ───────────────────────────────
function GradientTitle() {
  return (
    <h1 className="text-5xl md:text-6xl font-black text-center leading-tight">
      <span className="bg-gradient-to-r from-violet-400 via-pink-400 to-blue-400 bg-clip-text text-transparent bg-[length:200%_auto] animate-gradient">
        ValiantXP
      </span>
      <br />
      <span className="text-white text-4xl md:text-5xl">Demo Interactiva</span>
    </h1>
  )
}

// ── Prize Win Demo ───────────────────────────────────────
function PrizeWinDemo() {
  const [state, setState] = useState<'idle' | 'win'>('idle')
  const [confetti] = useState(() => Array.from({ length: 40 }, (_, i) => i))

  const colors = ['#8b5cf6', '#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#ec4899', '#06b6d4']

  return (
    <div className="text-center space-y-6">
      {state === 'idle' ? (
        <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
          <p className="text-zinc-400 mb-6 text-sm">Simula ganar un premio instantáneo</p>
          <button
            onClick={() => setState('win')}
            className="px-8 py-5 rounded-2xl bg-gradient-to-br from-violet-600 to-blue-600 text-white font-bold text-lg shadow-xl shadow-violet-500/30 hover:shadow-violet-500/50 hover:scale-105 active:scale-95 transition-all"
          >
            🎰 ¡Girar!
          </button>
        </motion.div>
      ) : (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          className="relative"
        >
          {/* Confetti */}
          <div className="absolute inset-0 overflow-hidden pointer-events-none flex items-center justify-center">
            {confetti.map((i) => {
              const angle = (i / confetti.length) * Math.PI * 2
              const r = 80 + Math.random() * 80
              return (
                <motion.div
                  key={i}
                  className="absolute w-2 h-2 rounded-sm"
                  style={{ backgroundColor: colors[i % colors.length] }}
                  initial={{ x: 0, y: 0, opacity: 1, rotate: 0, scale: 1 }}
                  animate={{
                    x: Math.cos(angle) * r * (1.5 + Math.random()),
                    y: Math.sin(angle) * r * (1.5 + Math.random()) + 100,
                    opacity: 0,
                    rotate: Math.random() * 720,
                    scale: 0,
                  }}
                  transition={{ duration: 1.2, delay: i * 0.02, ease: 'easeOut' }}
                />
              )
            })}
          </div>

          <motion.div
            animate={{ rotate: [0, -15, 15, -8, 8, 0], scale: [1, 1.3, 1] }}
            transition={{ duration: 0.7 }}
            className="text-8xl mb-6"
          >
            🏆
          </motion.div>

          <motion.div
            initial={{ y: 30, opacity: 0 }}
            animate={{ y: 0, opacity: 1 }}
            transition={{ delay: 0.3 }}
            className="space-y-4"
          >
            <h2 className="text-3xl font-black text-white">¡Felicitaciones!</h2>
            <div className="max-w-xs mx-auto p-6 rounded-2xl bg-gradient-to-br from-violet-600/20 to-blue-600/20 border border-violet-500/40">
              <p className="text-violet-300 font-bold text-lg">Bonus Especial</p>
              <p className="text-white font-mono text-3xl font-black mt-1">+500 pts</p>
            </div>
            <button
              onClick={() => setState('idle')}
              className="text-sm text-zinc-500 hover:text-zinc-400 underline underline-offset-2 transition-colors"
            >
              Intentar de nuevo
            </button>
          </motion.div>
        </motion.div>
      )}
    </div>
  )
}

// ── Guest Flow Demo ──────────────────────────────────────
function GuestFlowDemo() {
  const [showModal, setShowModal] = useState(false)

  return (
    <div className="space-y-6">
      <p className="text-zinc-400 text-sm text-center">
        Así ve un participante anónimo la experiencia de invitado
      </p>

      {/* Simulated banner */}
      <div className="rounded-xl overflow-hidden border border-amber-500/20">
        <div className="flex items-center justify-between gap-4 px-4 py-2.5 bg-amber-500/10 text-amber-300 text-sm">
          <div className="flex items-center gap-2">
            <span>⚠️</span>
            <span>Participando como invitado · Regístrate para guardar tus premios</span>
          </div>
          <button className="font-semibold text-amber-200 hover:text-white underline whitespace-nowrap">
            → Registrarme
          </button>
        </div>
      </div>

      {/* Demo challenge area */}
      <div className="p-6 rounded-2xl bg-white/5 border border-white/10 text-center space-y-4">
        <div className="text-4xl">🎮</div>
        <h3 className="font-bold text-white">Desafío disponible sin registro</h3>
        <p className="text-zinc-500 text-sm max-w-xs mx-auto">
          El usuario puede participar y ganar premios, pero necesita registrarse para reclamarlos
        </p>
        <button
          onClick={() => setShowModal(true)}
          className="px-6 py-3 rounded-xl bg-violet-600 hover:bg-violet-500 text-white font-semibold transition-all shadow-lg shadow-violet-500/20"
        >
          Simular premio ganado →
        </button>
      </div>

      {/* Prize claim modal */}
      <AnimatePresence>
        {showModal && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
            onClick={() => setShowModal(false)}
          >
            <motion.div
              initial={{ scale: 0.85, y: 20 }}
              animate={{ scale: 1, y: 0 }}
              exit={{ scale: 0.85, opacity: 0 }}
              onClick={(e) => e.stopPropagation()}
              className="max-w-sm w-full bg-zinc-900/95 backdrop-blur-xl border border-white/10 rounded-3xl p-8 text-center shadow-2xl"
            >
              <div className="text-5xl mb-4">🎁</div>
              <h2 className="text-2xl font-black text-white mb-2">¡Ganaste un premio!</h2>
              <p className="text-zinc-400 text-sm mb-2">
                Has ganado <span className="text-violet-400 font-bold">Gift Card $500</span>
              </p>
              <p className="text-zinc-600 text-xs mb-6">
                Regístrate para reclamar tu premio antes de que expire
              </p>
              <div className="space-y-3">
                <button className="w-full py-3 rounded-xl bg-violet-600 hover:bg-violet-500 text-white font-bold transition-all shadow-lg shadow-violet-500/25">
                  Registrarme y reclamar 🏆
                </button>
                <button
                  onClick={() => setShowModal(false)}
                  className="w-full py-3 rounded-xl border border-white/10 text-zinc-500 hover:text-zinc-400 text-sm transition-colors"
                >
                  Quizás después (perderé mi premio)
                </button>
              </div>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}

// ── Main DemoPage ────────────────────────────────────────
export default function DemoPage() {
  const [activeTab, setActiveTab] = useState('trivia')
  const active = tabs.find((t) => t.id === activeTab)!

  return (
    <div className="min-h-screen bg-zinc-950 relative overflow-hidden">
      {/* Background */}
      <div className="absolute inset-0 bg-gradient-to-br from-violet-900/10 via-zinc-950 to-blue-900/10 pointer-events-none" />
      <div className="absolute top-0 left-1/2 -translate-x-1/2 w-[800px] h-[400px] bg-violet-600/5 rounded-full blur-3xl pointer-events-none" />

      <div className="relative z-10 max-w-5xl mx-auto px-4 py-12">
        {/* Hero header */}
        <motion.div
          initial={{ opacity: 0, y: 30 }}
          animate={{ opacity: 1, y: 0 }}
          className="text-center mb-16"
        >
          <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-violet-500/10 border border-violet-500/20 text-violet-300 text-xs font-semibold mb-6">
            <Zap className="w-3 h-3" />
            Demo interactiva — Sin backend
          </div>

          <GradientTitle />

          <p className="text-zinc-400 text-lg mt-4 max-w-xl mx-auto">
            Explora todos los tipos de desafíos, flujos de premio y experiencia de usuario.
            Funciona al 100% sin servidor.
          </p>

          {/* Micro stats */}
          <div className="flex items-center justify-center gap-8 mt-8">
            {[
              { icon: Brain, label: '3 tipos de challenge', color: 'text-blue-400' },
              { icon: Trophy, label: 'Premios con animación', color: 'text-violet-400' },
              { icon: Star, label: 'Sin backend requerido', color: 'text-amber-400' },
            ].map(({ icon: Icon, label, color }) => (
              <div key={label} className="flex items-center gap-2 text-sm text-zinc-500">
                <Icon className={cn('w-4 h-4', color)} />
                {label}
              </div>
            ))}
          </div>
        </motion.div>

        {/* Tab navigation */}
        <div className="flex gap-2 flex-wrap justify-center mb-8">
          {tabs.map(({ id, label, icon: Icon, color, badgeColor, badge }) => (
            <button
              key={id}
              onClick={() => setActiveTab(id)}
              className={cn(
                'flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-semibold transition-all',
                activeTab === id
                  ? 'bg-white/10 text-white border border-white/20 shadow-lg'
                  : 'text-zinc-500 hover:text-zinc-300 hover:bg-white/5 border border-transparent'
              )}
            >
              <Icon className={cn('w-4 h-4', activeTab === id ? color : '')} />
              {label}
              {activeTab === id && (
                <span className={cn('text-xs px-2 py-0.5 rounded-full border font-bold', badgeColor)}>
                  {badge}
                </span>
              )}
            </button>
          ))}
        </div>

        {/* Active section */}
        <AnimatePresence mode="wait">
          <motion.div
            key={activeTab}
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -10 }}
            transition={{ duration: 0.25 }}
          >
            {/* Section header */}
            <div className="text-center mb-8">
              <div className="flex items-center justify-center gap-3 mb-2">
                <active.icon className={cn('w-6 h-6', active.color)} />
                <h2 className="text-2xl font-bold text-white">{active.label}</h2>
                <span className={cn('text-xs px-3 py-1 rounded-full border font-bold', active.badgeColor)}>
                  {active.badge}
                </span>
              </div>
              <p className="text-zinc-500 text-sm">{active.desc}</p>
            </div>

            {/* Challenge container */}
            <div className="max-w-2xl mx-auto bg-white/5 backdrop-blur-sm border border-white/10 rounded-3xl p-8 shadow-2xl shadow-black/30">
              {activeTab === 'trivia' && (
                <TriviaChallenge config={DEMO_TRIVIA} demoMode />
              )}
              {activeTab === 'survey' && (
                <SurveyChallenge config={DEMO_SURVEY} demoMode />
              )}
              {activeTab === 'code' && (
                <CodeChallenge
                  onSubmit={async (code) => {
                    // Demo: simula respuesta position-based
                    await new Promise((r) => setTimeout(r, 1200))
                    const upper = code.trim().toUpperCase()
                    const isWinner = upper === 'DEMO2024' || upper === 'MESA001'
                    return {
                      success: true,
                      message: isWinner
                        ? 'Posición #3 del día. ¡Posición ganadora!'
                        : 'Posición #7 del día.',
                      nextChallengeId: isWinner ? undefined : undefined,
                      payload: {
                        Position: isWinner ? 3 : 7,
                        IsWinner: isWinner,
                        DailyCount: isWinner ? 3 : 7,
                        PositionBased: true,
                        PrizeTier: isWinner ? 'silver' : undefined,
                      },
                    }
                  }}
                />
              )}
              {activeTab === 'prize' && (
                <PrizeWinDemo />
              )}
              {activeTab === 'giftcard' && (
                <GiftCardReveal
                  code="VXDEMO-2024-GIFT"
                  redeemUrl="https://example.com/redeem"
                  prizeName="Gift Card Demo — $500"
                />
              )}
              {activeTab === 'guest' && (
                <GuestFlowDemo />
              )}
            </div>
          </motion.div>
        </AnimatePresence>

        {/* Footer note */}
        <p className="text-center text-xs text-zinc-700 mt-12">
          ValiantXP Demo · Todos los datos son de ejemplo
        </p>
      </div>

      {/* Gradient animation style */}
      <style>{`
        @keyframes gradient {
          0% { background-position: 0% 50%; }
          50% { background-position: 100% 50%; }
          100% { background-position: 0% 50%; }
        }
        .animate-gradient {
          animation: gradient 4s ease infinite;
        }
      `}</style>
    </div>
  )
}

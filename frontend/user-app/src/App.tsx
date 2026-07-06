import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { queryClient } from '@/lib/queryClient'
import LoginPage from '@/pages/LoginPage'
import OAuthCallbackPage from '@/pages/OAuthCallbackPage'
import LandingPage from '@/pages/LandingPage'
import ChallengePage from '@/pages/ChallengePage'
import PrizesPage from '@/pages/PrizesPage'
import ProfilePage from '@/pages/ProfilePage'
import DemoPage from '@/pages/DemoPage'
import { ProtectedRoute } from '@/components/layout/ProtectedRoute'
import { AppLayout } from '@/components/layout/AppLayout'

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          {/* Public */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/auth/:provider/callback" element={<OAuthCallbackPage />} />
          <Route path="/demo" element={<DemoPage />} />

          {/* Protected — wrapped in AppLayout */}
          <Route
            element={
              <ProtectedRoute>
                <AppLayout />
              </ProtectedRoute>
            }
          >
            <Route path="/" element={<LandingPage />} />
            <Route path="/challenge/:id" element={<ChallengePage />} />
            <Route path="/prizes" element={<PrizesPage />} />
            <Route path="/profile" element={<ProfilePage />} />
          </Route>

          {/* Fallback */}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  )
}

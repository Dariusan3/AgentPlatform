import { Suspense, lazy } from 'react'
import { QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { ProtectedRoute } from '@/components/ProtectedRoute'
import { PublicOnlyRoute } from '@/components/PublicOnlyRoute'
import { FullPageSpinner } from '@/components/Spinner'
import { Toaster } from '@/components/ui/sonner'
import { AuthProvider } from '@/context/AuthContext'
import { queryClient } from '@/lib/queryClient'
import { LandingPage } from '@/pages/LandingPage'
import { LoginPage } from '@/pages/LoginPage'
import { SignupPage } from '@/pages/SignupPage'

/**
 * Dashboardul se incarca separat. Recharts si primitivele Radix sunt grele, iar
 * fara despicare ar intarzia si landing page-ul, care e pagina de marketing.
 */
const DashboardLayout = lazy(() =>
  import('@/components/layout/DashboardLayout').then((m) => ({
    default: m.DashboardLayout,
  })),
)
const DashboardPage = lazy(() =>
  import('@/pages/DashboardPage').then((m) => ({ default: m.DashboardPage })),
)
const PropertiesPage = lazy(() =>
  import('@/pages/PropertiesPage').then((m) => ({ default: m.PropertiesPage })),
)
const AgentsPage = lazy(() =>
  import('@/pages/AgentsPage').then((m) => ({ default: m.AgentsPage })),
)
const VoiceAgentsPage = lazy(() =>
  import('@/pages/VoiceAgentsPage').then((m) => ({
    default: m.VoiceAgentsPage,
  })),
)
const LeadsPage = lazy(() =>
  import('@/pages/LeadsPage').then((m) => ({ default: m.LeadsPage })),
)
const ConversationsPage = lazy(() =>
  import('@/pages/ConversationsPage').then((m) => ({
    default: m.ConversationsPage,
  })),
)
const SettingsPage = lazy(() =>
  import('@/pages/SettingsPage').then((m) => ({ default: m.SettingsPage })),
)

export default function App() {
  return (
    // BrowserRouter e in exterior: signOut din AuthProvider foloseste useNavigate
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <Toaster />
          <Suspense fallback={<FullPageSpinner />}>
            <Routes>
              <Route path="/" element={<LandingPage />} />

              <Route
                path="/login"
                element={
                  <PublicOnlyRoute>
                    <LoginPage />
                  </PublicOnlyRoute>
                }
              />
              <Route
                path="/signup"
                element={
                  <PublicOnlyRoute>
                    <SignupPage />
                  </PublicOnlyRoute>
                }
              />

              <Route
                path="/dashboard"
                element={
                  <ProtectedRoute>
                    <DashboardLayout />
                  </ProtectedRoute>
                }
              >
                <Route index element={<DashboardPage />} />
                <Route path="properties" element={<PropertiesPage />} />
                <Route path="agents" element={<AgentsPage />} />
                <Route path="voice-agents" element={<VoiceAgentsPage />} />
                <Route path="leads" element={<LeadsPage />} />
                <Route path="conversations" element={<ConversationsPage />} />
                <Route path="settings" element={<SettingsPage />} />
                {/* Orice /dashboard/ceva inexistent cade pe panoul principal */}
                <Route path="*" element={<Navigate to="/dashboard" replace />} />
              </Route>

              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </Suspense>
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  )
}

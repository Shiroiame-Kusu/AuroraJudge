import { Routes, Route, Navigate } from 'react-router-dom'
import { Suspense, lazy, useEffect, useState } from 'react'
import { Button, Card, Result } from 'antd'
import { LoadingSpinner } from './components'
import MainLayout from './layouts/MainLayout'
import AuthGuard from './components/AuthGuard'
import setupService from './services/setupService'

// 懒加载页面组件
const HomePage = lazy(() => import('./pages/Home'))
const LoginPage = lazy(() => import('./pages/Login'))
const RegisterPage = lazy(() => import('./pages/Register'))
const ProblemsPage = lazy(() => import('./pages/Problems'))
const ProblemDetailPage = lazy(() => import('./pages/ProblemDetail'))
const SubmissionsPage = lazy(() => import('./pages/Submissions'))
const SubmissionDetailPage = lazy(() => import('./pages/SubmissionDetail'))
const ContestsPage = lazy(() => import('./pages/Contests'))
const ContestDetailPage = lazy(() => import('./pages/ContestDetail'))
const RankingsPage = lazy(() => import('./pages/Rankings'))
const ProfilePage = lazy(() => import('./pages/Profile'))
const AdminPage = lazy(() => import('./pages/Admin'))
const SetupPage = lazy(() => import('./pages/Setup'))

const Loading = () => (
  <div className="flex items-center justify-center h-screen">
    <LoadingSpinner size="large" />
  </div>
)

// Setup 检测组件
const SetupCheck = ({ children }: { children: React.ReactNode }) => {
  const [checking, setChecking] = useState(true)
  const [needsSetup, setNeedsSetup] = useState(false)
  const [backendUnavailable, setBackendUnavailable] = useState(false)

  useEffect(() => {
    checkSetupStatus()
  }, [])

  const checkSetupStatus = async () => {
    setChecking(true)
    setBackendUnavailable(false)
    try {
      const status = await setupService.getStatus()
      setNeedsSetup(status.needsSetup)
    } catch {
      // 后端未启动/无法连接时，不要跳转到 /setup，避免“误判未初始化”
      setBackendUnavailable(true)
    } finally {
      setChecking(false)
    }
  }

  if (checking) {
    return <Loading />
  }

  if (backendUnavailable) {
    return (
      <div className="flex items-center justify-center min-h-screen p-4">
        <Card className="w-full max-w-xl">
          <Result
            status="warning"
            title="后端未启动或无法连接"
            subTitle="请先启动后端服务（或检查端口/代理配置），然后点击重试。"
            extra={
              <Button type="primary" onClick={checkSetupStatus}>
                重试
              </Button>
            }
          />
        </Card>
      </div>
    )
  }

  if (needsSetup) {
    return <Navigate to="/setup" replace />
  }

  return <>{children}</>
}

function App() {
  return (
    <Suspense fallback={<Loading />}>
      <Routes>
        {/* Setup 页面不需要检测 */}
        <Route path="/setup" element={<SetupPage />} />
        
        {/* 其他页面需要检测是否已完成设置 */}
        <Route path="/login" element={
          <SetupCheck>
            <LoginPage />
          </SetupCheck>
        } />
        <Route path="/register" element={
          <SetupCheck>
            <RegisterPage />
          </SetupCheck>
        } />
        
        <Route element={<MainLayout />}>
          <Route path="/" element={
            <SetupCheck>
              <HomePage />
            </SetupCheck>
          } />
          <Route path="/problems" element={
            <SetupCheck>
              <ProblemsPage />
            </SetupCheck>
          } />
          <Route path="/problems/:id" element={
            <SetupCheck>
              <ProblemDetailPage />
            </SetupCheck>
          } />
          <Route path="/submissions" element={
            <SetupCheck>
              <SubmissionsPage />
            </SetupCheck>
          } />
          <Route path="/submissions/:id" element={
            <SetupCheck>
              <SubmissionDetailPage />
            </SetupCheck>
          } />
          <Route path="/contests" element={
            <SetupCheck>
              <ContestsPage />
            </SetupCheck>
          } />
          <Route path="/contests/:id" element={
            <SetupCheck>
              <ContestDetailPage />
            </SetupCheck>
          } />
          <Route path="/rankings" element={
            <SetupCheck>
              <RankingsPage />
            </SetupCheck>
          } />
          
          <Route element={<AuthGuard />}>
            <Route path="/profile" element={<ProfilePage />} />
          </Route>
          
          <Route element={<AuthGuard requireAdmin />}>
            <Route path="/admin/*" element={<AdminPage />} />
          </Route>
        </Route>
      </Routes>
    </Suspense>
  )
}

export default App

import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuthStore } from '@/stores'
import { Result, Button } from 'antd'

interface AuthGuardProps {
  requireAdmin?: boolean
}

const AuthGuard = ({ requireAdmin = false }: AuthGuardProps) => {
  const location = useLocation()
  const { isAuthenticated, isAdmin } = useAuthStore()
  
  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }
  
  if (requireAdmin && !isAdmin()) {
    return (
      <Result
        status="403"
        title="403"
        subTitle="抱歉，您没有权限访问此页面"
        extra={
          <Button type="primary" onClick={() => window.history.back()}>
            返回
          </Button>
        }
      />
    )
  }
  
  return <Outlet />
}

export default AuthGuard

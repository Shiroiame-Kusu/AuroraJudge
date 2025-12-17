import { useState } from 'react'
import { Form, Input, Button, Card, Typography, message, Divider } from 'antd'
import { UserOutlined, LockOutlined } from '@ant-design/icons'
import { Link, useNavigate, useLocation } from 'react-router-dom'
import { useAuthStore } from '@/stores'
import { authService } from '@/services'

const { Title, Text } = Typography

interface LoginForm {
  username: string
  password: string
}

const LoginPage = () => {
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()
  const location = useLocation()
  const { setAuth } = useAuthStore()
  
  const from = (location.state as { from?: Location })?.from?.pathname || '/'
  
  const handleSubmit = async (values: LoginForm) => {
    setLoading(true)
    try {
      const response = await authService.login({
        usernameOrEmail: values.username,
        password: values.password,
      })
      if (response.success && response.data) {
        const { user, accessToken, refreshToken } = response.data
        setAuth(user, accessToken, refreshToken)
        message.success('登录成功')
        navigate(from, { replace: true })
      }
    } catch (error: any) {
      message.error(error.message || '登录失败')
    } finally {
      setLoading(false)
    }
  }
  
  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4">
      <Card className="w-full max-w-md">
        <div className="text-center mb-8">
          <Title level={2}>Aurora Judge</Title>
          <Text type="secondary">登录您的账号</Text>
        </div>
        
        <Form
          name="login"
          onFinish={handleSubmit}
          size="large"
          autoComplete="off"
        >
          <Form.Item
            name="username"
            rules={[{ required: true, message: '请输入用户名' }]}
          >
            <Input 
              prefix={<UserOutlined />} 
              placeholder="用户名" 
            />
          </Form.Item>
          
          <Form.Item
            name="password"
            rules={[{ required: true, message: '请输入密码' }]}
          >
            <Input.Password 
              prefix={<LockOutlined />} 
              placeholder="密码" 
            />
          </Form.Item>
          
          <Form.Item>
            <Button type="primary" htmlType="submit" loading={loading} block>
              登录
            </Button>
          </Form.Item>
        </Form>
        
        <Divider plain>
          <Text type="secondary">还没有账号？</Text>
        </Divider>
        
        <Link to="/register">
          <Button block>注册新账号</Button>
        </Link>
      </Card>
    </div>
  )
}

export default LoginPage

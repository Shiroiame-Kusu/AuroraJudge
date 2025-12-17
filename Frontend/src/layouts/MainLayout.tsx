import { Outlet, Link, useNavigate, useLocation } from 'react-router-dom'
import { Layout, Menu, Avatar, Dropdown, Button, Space } from 'antd'
import {
  HomeOutlined,
  CodeOutlined,
  FileTextOutlined,
  TrophyOutlined,
  OrderedListOutlined,
  UserOutlined,
  SettingOutlined,
  LogoutOutlined,
  LoginOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
} from '@ant-design/icons'
import { useAuthStore, useUIStore } from '@/stores'
import type { MenuProps } from 'antd'

const { Header, Sider, Content } = Layout

const MainLayout = () => {
  const navigate = useNavigate()
  const location = useLocation()
  const { user, isAuthenticated, clearAuth, isAdmin } = useAuthStore()
  const { sidebarCollapsed, toggleSidebar } = useUIStore()
  
  const menuItems: MenuProps['items'] = [
    {
      key: '/',
      icon: <HomeOutlined />,
      label: <Link to="/">首页</Link>,
    },
    {
      key: '/problems',
      icon: <CodeOutlined />,
      label: <Link to="/problems">题库</Link>,
    },
    {
      key: '/submissions',
      icon: <FileTextOutlined />,
      label: <Link to="/submissions">提交记录</Link>,
    },
    {
      key: '/contests',
      icon: <TrophyOutlined />,
      label: <Link to="/contests">比赛</Link>,
    },
    {
      key: '/rankings',
      icon: <OrderedListOutlined />,
      label: <Link to="/rankings">排行榜</Link>,
    },
  ]
  
  const handleLogout = () => {
    clearAuth()
    navigate('/login')
  }
  
  const userMenuItems: MenuProps['items'] = [
    {
      key: 'profile',
      icon: <UserOutlined />,
      label: '个人中心',
      onClick: () => navigate('/profile'),
    },
    ...(isAdmin() ? [{
      key: 'admin',
      icon: <SettingOutlined />,
      label: '管理后台',
      onClick: () => navigate('/admin'),
    }] : []),
    {
      type: 'divider' as const,
    },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: '退出登录',
      onClick: handleLogout,
    },
  ]
  
  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider 
        trigger={null} 
        collapsible 
        collapsed={sidebarCollapsed}
        theme="light"
        style={{
          overflow: 'auto',
          height: '100vh',
          position: 'fixed',
          left: 0,
          top: 0,
          bottom: 0,
          borderRight: '1px solid #f0f0f0',
        }}
      >
        <div className="flex items-center justify-center h-16 border-b border-gray-100">
          <Link to="/" className="flex items-center gap-2 text-primary-600 font-bold text-lg">
            {!sidebarCollapsed && <span>Aurora Judge</span>}
            {sidebarCollapsed && <span>AJ</span>}
          </Link>
        </div>
        <Menu
          mode="inline"
          selectedKeys={[location.pathname]}
          items={menuItems}
          style={{ borderRight: 0 }}
        />
      </Sider>
      
      <Layout style={{ marginLeft: sidebarCollapsed ? 80 : 200 }}>
        <Header 
          style={{ 
            padding: '0 24px',
            background: '#fff',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            borderBottom: '1px solid #f0f0f0',
          }}
        >
          <Button
            type="text"
            icon={sidebarCollapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
            onClick={toggleSidebar}
          />
          
          <Space size="middle">
            {isAuthenticated ? (
              <Dropdown menu={{ items: userMenuItems }} placement="bottomRight">
                <Space className="cursor-pointer">
                  <Avatar 
                    src={user?.avatar} 
                    icon={<UserOutlined />}
                    size="small"
                  />
                  <span>{user?.nickname || user?.username}</span>
                </Space>
              </Dropdown>
            ) : (
              <Space>
                <Button type="text" icon={<LoginOutlined />} onClick={() => navigate('/login')}>
                  登录
                </Button>
                <Button type="primary" onClick={() => navigate('/register')}>
                  注册
                </Button>
              </Space>
            )}
          </Space>
        </Header>
        
        <Content style={{ margin: '24px', minHeight: 280 }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  )
}

export default MainLayout

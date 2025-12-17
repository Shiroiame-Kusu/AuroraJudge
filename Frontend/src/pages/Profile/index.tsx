import { Card, Tabs, Form, Input, Button, message, Descriptions, Avatar } from 'antd'
import { UserOutlined } from '@ant-design/icons'
import { useAuthStore } from '@/stores'
import { authService } from '@/services'
import { useState } from 'react'

const ProfilePage = () => {
  const { user } = useAuthStore()
  const [passwordLoading, setPasswordLoading] = useState(false)
  const [passwordForm] = Form.useForm()
  
  const handleChangePassword = async (values: { currentPassword: string; newPassword: string; confirmPassword: string }) => {
    setPasswordLoading(true)
    try {
      await authService.changePassword(values)
      message.success('密码修改成功')
      passwordForm.resetFields()
    } catch (error: any) {
      message.error(error.message || '修改失败')
    } finally {
      setPasswordLoading(false)
    }
  }
  
  return (
    <Card title="个人中心">
      <Tabs
        items={[
          {
            key: 'profile',
            label: '基本信息',
            children: (
              <div className="max-w-2xl">
                <div className="flex items-center gap-4 mb-6">
                  <Avatar
                    size={80}
                    src={user?.avatar}
                    icon={<UserOutlined />}
                  />
                  <div>
                    <h2 className="text-xl font-bold">{user?.nickname || user?.username}</h2>
                    <p className="text-gray-500">@{user?.username}</p>
                  </div>
                </div>
                
                <Descriptions bordered column={1}>
                  <Descriptions.Item label="用户名">{user?.username}</Descriptions.Item>
                  <Descriptions.Item label="昵称">{user?.nickname || '-'}</Descriptions.Item>
                  <Descriptions.Item label="邮箱">{user?.email}</Descriptions.Item>
                  <Descriptions.Item label="角色">
                    {user?.roles?.join(', ') || '-'}
                  </Descriptions.Item>
                </Descriptions>
              </div>
            ),
          },
          {
            key: 'password',
            label: '修改密码',
            children: (
              <div className="max-w-md">
                <Form
                  form={passwordForm}
                  layout="vertical"
                  onFinish={handleChangePassword}
                >
                  <Form.Item
                    name="currentPassword"
                    label="当前密码"
                    rules={[{ required: true, message: '请输入当前密码' }]}
                  >
                    <Input.Password />
                  </Form.Item>
                  
                  <Form.Item
                    name="newPassword"
                    label="新密码"
                    rules={[
                      { required: true, message: '请输入新密码' },
                      { min: 8, message: '密码至少 8 个字符' },
                    ]}
                  >
                    <Input.Password />
                  </Form.Item>
                  
                  <Form.Item
                    name="confirmPassword"
                    label="确认新密码"
                    dependencies={['newPassword']}
                    rules={[
                      { required: true, message: '请确认新密码' },
                      ({ getFieldValue }) => ({
                        validator(_, value) {
                          if (!value || getFieldValue('newPassword') === value) {
                            return Promise.resolve()
                          }
                          return Promise.reject(new Error('两次输入的密码不一致'))
                        },
                      }),
                    ]}
                  >
                    <Input.Password />
                  </Form.Item>
                  
                  <Form.Item>
                    <Button type="primary" htmlType="submit" loading={passwordLoading}>
                      修改密码
                    </Button>
                  </Form.Item>
                </Form>
              </div>
            ),
          },
        ]}
      />
    </Card>
  )
}

export default ProfilePage

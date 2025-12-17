import { useState, useEffect } from 'react'
import { 
  Steps, 
  Card, 
  Form, 
  Input, 
  Button, 
  InputNumber, 
  Switch, 
  Space, 
  Typography, 
  Alert, 
  Result, 
  message, 
  Divider,
  Select,
  Collapse,
  Row,
  Col
} from 'antd'
import { 
  DatabaseOutlined, 
  UserOutlined, 
  CloudServerOutlined, 
  SettingOutlined,
  CheckCircleOutlined,
  LoadingOutlined,
  CopyOutlined,
  SafetyOutlined
} from '@ant-design/icons'
import setupService, { 
  SetupRequest, 
  DatabaseConfig, 
  SetupStatus 
} from '@/services/setupService'
import { useNavigate } from 'react-router-dom'
import { LoadingSpinner } from '@/components'

const { Title, Text, Paragraph } = Typography
const { Password } = Input
const { Panel } = Collapse

const SetupPage = () => {
  const navigate = useNavigate()
  const [currentStep, setCurrentStep] = useState(0)
  const [loading, setLoading] = useState(true)
  const [statusError, setStatusError] = useState<string | null>(null)
  const [initializing, setInitializing] = useState(false)
  const [, setSetupStatus] = useState<SetupStatus | null>(null)
  const [setupComplete, setSetupComplete] = useState(false)
  const [judgerCredentials, setJudgerCredentials] = useState<{ name: string; secret: string } | null>(null)

  // 表单
  const [databaseForm] = Form.useForm()
  const [adminForm] = Form.useForm()
  const [judgerForm] = Form.useForm()
  const [configForm] = Form.useForm()

  // 表单数据
  const [databaseConfig, setDatabaseConfig] = useState<DatabaseConfig | null>(null)
  const [adminSnapshot, setAdminSnapshot] = useState<any | null>(null)
  const [judgerSnapshot, setJudgerSnapshot] = useState<any | null>(null)
  const [dbTestResult, setDbTestResult] = useState<{ success: boolean; message: string } | null>(null)
  const [testingDb, setTestingDb] = useState(false)

  // 检查初始化状态
  useEffect(() => {
    checkStatus()
  }, [])

  const checkStatus = async () => {
    try {
      setStatusError(null)
      const status = await setupService.getStatus()
      setSetupStatus(status)
      
      if (!status.needsSetup) {
        // 已经初始化过，跳转到首页
        navigate('/')
      }
      
      // 如果有现有配置，预填表单
      if (status.currentConfig) {
        const config = status.currentConfig
        
        databaseForm.setFieldsValue({
          host: config.databaseHost || 'localhost',
          port: config.databasePort || 5432,
          database: config.databaseName || 'aurora_judge',
          username: config.databaseUser || 'postgres',
        })
        
        configForm.setFieldsValue({
          siteName: 'Aurora Judge',
          siteDescription: '一个现代化的在线评测系统',
          allowRegistration: true,
          serverPort: 5000,
          useHttps: false,
          corsOrigins: 'http://localhost:3000',
          redisEnabled: false,
          redisConnection: '',
          storageType: config.storageType?.toLowerCase() || 'local',
          localPath: config.storagePath || './data',
        })
      }
    } catch (error) {
      setStatusError('获取系统状态失败：后端未启动或无法连接')
    } finally {
      setLoading(false)
    }
  }

  if (!loading && statusError) {
    return (
      <div className="min-h-screen flex items-center justify-center p-4">
        <Card className="w-full max-w-xl">
          <Result
            status="warning"
            title="无法连接后端"
            subTitle={statusError}
            extra={
              <Button type="primary" onClick={checkStatus}>
                重试
              </Button>
            }
          />
        </Card>
      </div>
    )
  }

  // 测试数据库连接
  const handleTestDatabase = async () => {
    try {
      const values = await databaseForm.validateFields()
      setTestingDb(true)
      setDbTestResult(null)
      
      const result = await setupService.testDatabase(values)
      setDbTestResult(result)
      
      if (result.success) {
        message.success('数据库连接成功')
        setDatabaseConfig(values)
      } else {
        message.error(result.message)
      }
    } catch (error: any) {
      setDbTestResult({ success: false, message: error.message || '连接测试失败' })
      message.error(error.message || '连接测试失败')
    } finally {
      setTestingDb(false)
    }
  }

  // 下一步
  const handleNext = async () => {
    try {
      switch (currentStep) {
        case 0: // 数据库配置
          const dbValues = await databaseForm.validateFields()
          if (!dbTestResult?.success) {
            message.warning('请先测试数据库连接')
            return
          }
          setDatabaseConfig(dbValues)
          break
        case 1: // 管理员配置
          const adminValues = await adminForm.validateFields()
          setAdminSnapshot(adminValues)
          break
        case 2: // Judger 配置
          const judgerValues = await judgerForm.validateFields()
          setJudgerSnapshot(judgerValues)
          break
      }
      setCurrentStep(currentStep + 1)
    } catch {
      // 表单验证失败
    }
  }

  // 上一步
  const handlePrev = () => {
    setCurrentStep(currentStep - 1)
  }

  // 执行初始化
  const handleInitialize = async () => {
    try {
      // 注意：管理员/Judger 表单在最终步骤时通常已卸载。
      // validateFields() 会返回空对象（字段未注册），导致请求缺失字段并触发后端默认值（例如创建 "admin"）。
      // 因此这里使用在“下一步”时缓存的快照。
      const adminValues = adminSnapshot
      const judgerValues = judgerSnapshot
      const configValues = await configForm.validateFields()
      
      if (!databaseConfig) {
        message.error('请先配置数据库')
        setCurrentStep(0)
        return
      }

      if (!adminValues) {
        message.error('请先完成管理员配置')
        setCurrentStep(1)
        return
      }

      if (!judgerValues) {
        message.error('请先完成 Judger 配置')
        setCurrentStep(2)
        return
      }

      setInitializing(true)

      const request: SetupRequest = {
        database: databaseConfig,
        admin: {
          username: adminValues.username,
          email: adminValues.email,
          password: adminValues.password,
          displayName: adminValues.displayName || adminValues.username,
        },
        judger: {
          name: judgerValues.judgerName || 'default-judger',
          description: judgerValues.judgerDescription,
          maxConcurrentTasks: judgerValues.maxConcurrentTasks || 4,
        },
        site: {
          name: configValues.siteName,
          description: configValues.siteDescription,
          allowRegister: configValues.allowRegistration,
        },
        redis: {
          connection: configValues.redisEnabled ? configValues.redisConnection : undefined,
        },
        storage: {
          type: configValues.storageType === 'minio' ? 'Minio' : 'Local',
          localPath: configValues.localPath,
          minio: configValues.storageType === 'minio' ? {
            endpoint: configValues.minioEndpoint,
            accessKey: configValues.minioAccessKey,
            secretKey: configValues.minioSecretKey,
            bucket: configValues.minioBucket,
            useSsl: configValues.minioUseSSL,
          } : undefined,
        },
        cors: {
          origins: configValues.corsOrigins || undefined,
        },
        server: {
          environment: configValues.environment || undefined,
          urls: `${configValues.useHttps ? 'https' : 'http'}://+:${configValues.serverPort}`,
        },
      }

      const result = await setupService.initialize(request)
      
      if (result.success) {
        setSetupComplete(true)
        setJudgerCredentials(result.judgerCredentials || null)
        message.success(result.message || '系统初始化成功！')
      } else {
        message.error(result.message)
      }
    } catch (error: any) {
      message.error(error.message || '初始化失败')
    } finally {
      setInitializing(false)
    }
  }

  // 复制到剪贴板
  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text)
    message.success('已复制到剪贴板')
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-gray-100">
        <LoadingSpinner size="large" tip="检查系统状态..." />
      </div>
    )
  }

  // 初始化完成
  if (setupComplete) {
    return (
      <div className="min-h-screen bg-gray-100 py-12 px-4">
        <Card className="max-w-2xl mx-auto">
          <Result
            status="success"
            icon={<CheckCircleOutlined />}
            title="系统初始化完成！"
            subTitle="Aurora Judge 已成功配置，您现在可以开始使用了。"
            extra={[
              <Button type="primary" key="login" onClick={() => navigate('/login')}>
                前往登录
              </Button>,
            ]}
          >
            {judgerCredentials && (
              <div className="mt-6">
                <Alert
                  type="warning"
                  showIcon
                  icon={<SafetyOutlined />}
                  message="重要：请保存 Judger 凭据"
                  description={
                    <div className="mt-2">
                      <Paragraph>
                        以下是 Judger 节点的连接凭据，<Text strong type="danger">请务必妥善保存，此信息只显示一次！</Text>
                      </Paragraph>
                      <div className="bg-gray-100 p-4 rounded mt-2">
                        <div className="flex justify-between items-center mb-2">
                          <Text>Judger 名称：</Text>
                          <Space>
                            <Text code>{judgerCredentials.name}</Text>
                            <Button 
                              size="small" 
                              icon={<CopyOutlined />}
                              onClick={() => copyToClipboard(judgerCredentials.name)}
                            />
                          </Space>
                        </div>
                        <div className="flex justify-between items-center">
                          <Text>Judger 密钥：</Text>
                          <Space>
                            <Text code copyable>{judgerCredentials.secret}</Text>
                            <Button 
                              size="small" 
                              icon={<CopyOutlined />}
                              onClick={() => copyToClipboard(judgerCredentials.secret)}
                            />
                          </Space>
                        </div>
                      </div>
                      <Paragraph className="mt-2 text-gray-500">
                        请在 Judger 的 <Text code>judger.conf</Text> 配置文件中设置这些值。
                      </Paragraph>
                    </div>
                  }
                />
              </div>
            )}
          </Result>
        </Card>
      </div>
    )
  }

  const steps = [
    {
      title: '数据库',
      icon: <DatabaseOutlined />,
    },
    {
      title: '管理员',
      icon: <UserOutlined />,
    },
    {
      title: 'Judger',
      icon: <CloudServerOutlined />,
    },
    {
      title: '系统配置',
      icon: <SettingOutlined />,
    },
  ]

  return (
    <div className="min-h-screen bg-gray-100 py-8 px-4">
      <div className="max-w-4xl mx-auto">
        {/* 头部 */}
        <div className="text-center mb-8">
          <Title level={2}>Aurora Judge 初始化设置</Title>
          <Text type="secondary">首次使用前，请完成以下配置</Text>
        </div>

        {/* 步骤条 */}
        <Steps current={currentStep} items={steps} className="mb-8" />

        {/* 内容区 */}
        <Card className="shadow-sm">
          {/* Step 0: 数据库配置 */}
          {currentStep === 0 && (
            <div>
              <Title level={4}>
                <DatabaseOutlined className="mr-2" />
                数据库配置
              </Title>
              <Paragraph type="secondary" className="mb-6">
                配置 PostgreSQL 数据库连接信息。系统将自动创建所需的表结构。
              </Paragraph>

              <Form
                form={databaseForm}
                layout="vertical"
                initialValues={{
                  host: 'localhost',
                  port: 5432,
                  database: 'aurora_judge',
                  username: 'postgres',
                }}
              >
                <Row gutter={16}>
                  <Col span={16}>
                    <Form.Item
                      name="host"
                      label="主机地址"
                      rules={[{ required: true, message: '请输入数据库主机地址' }]}
                    >
                      <Input placeholder="localhost" />
                    </Form.Item>
                  </Col>
                  <Col span={8}>
                    <Form.Item
                      name="port"
                      label="端口"
                      rules={[{ required: true, message: '请输入端口' }]}
                    >
                      <InputNumber min={1} max={65535} className="w-full" />
                    </Form.Item>
                  </Col>
                </Row>

                <Form.Item
                  name="database"
                  label="数据库名"
                  rules={[{ required: true, message: '请输入数据库名' }]}
                >
                  <Input placeholder="aurora_judge" />
                </Form.Item>

                <Row gutter={16}>
                  <Col span={12}>
                    <Form.Item
                      name="username"
                      label="用户名"
                      rules={[{ required: true, message: '请输入数据库用户名' }]}
                    >
                      <Input placeholder="postgres" />
                    </Form.Item>
                  </Col>
                  <Col span={12}>
                    <Form.Item
                      name="password"
                      label="密码"
                      rules={[{ required: true, message: '请输入数据库密码' }]}
                    >
                      <Password placeholder="数据库密码" />
                    </Form.Item>
                  </Col>
                </Row>

                <div className="flex items-center gap-4">
                  <Button 
                    onClick={handleTestDatabase}
                    loading={testingDb}
                  >
                    测试连接
                  </Button>
                  {dbTestResult && (
                    <Text type={dbTestResult.success ? 'success' : 'danger'}>
                      {dbTestResult.success ? '✓ 连接成功' : `✗ ${dbTestResult.message}`}
                    </Text>
                  )}
                </div>
              </Form>
            </div>
          )}

          {/* Step 1: 管理员配置 */}
          {currentStep === 1 && (
            <div>
              <Title level={4}>
                <UserOutlined className="mr-2" />
                管理员账户
              </Title>
              <Paragraph type="secondary" className="mb-6">
                创建系统管理员账户，该账户将拥有全部权限。
              </Paragraph>

              <Form
                form={adminForm}
                layout="vertical"
                initialValues={{
                  username: 'admin',
                }}
              >
                <Row gutter={16}>
                  <Col span={12}>
                    <Form.Item
                      name="username"
                      label="用户名"
                      rules={[
                        { required: true, message: '请输入用户名' },
                        { min: 3, message: '用户名至少3个字符' },
                        { pattern: /^[a-zA-Z0-9_]+$/, message: '只能包含字母、数字和下划线' },
                      ]}
                    >
                      <Input placeholder="admin" />
                    </Form.Item>
                  </Col>
                  <Col span={12}>
                    <Form.Item
                      name="displayName"
                      label="显示名称"
                    >
                      <Input placeholder="Administrator" />
                    </Form.Item>
                  </Col>
                </Row>

                <Form.Item
                  name="email"
                  label="邮箱"
                  rules={[
                    { required: true, message: '请输入邮箱' },
                    { type: 'email', message: '请输入有效的邮箱地址' },
                  ]}
                >
                  <Input placeholder="admin@example.com" />
                </Form.Item>

                <Row gutter={16}>
                  <Col span={12}>
                    <Form.Item
                      name="password"
                      label="密码"
                      rules={[
                        { required: true, message: '请输入密码' },
                        { min: 8, message: '密码至少8个字符' },
                      ]}
                    >
                      <Password placeholder="至少8个字符" />
                    </Form.Item>
                  </Col>
                  <Col span={12}>
                    <Form.Item
                      name="confirmPassword"
                      label="确认密码"
                      dependencies={['password']}
                      rules={[
                        { required: true, message: '请确认密码' },
                        ({ getFieldValue }) => ({
                          validator(_, value) {
                            if (!value || getFieldValue('password') === value) {
                              return Promise.resolve()
                            }
                            return Promise.reject(new Error('两次输入的密码不一致'))
                          },
                        }),
                      ]}
                    >
                      <Password placeholder="再次输入密码" />
                    </Form.Item>
                  </Col>
                </Row>
              </Form>
            </div>
          )}

          {/* Step 2: Judger 配置 */}
          {currentStep === 2 && (
            <div>
              <Title level={4}>
                <CloudServerOutlined className="mr-2" />
                Judger 配置
              </Title>
              <Paragraph type="secondary" className="mb-6">
                配置默认的 Judger 节点。初始化完成后，您将获得该节点的连接密钥。
              </Paragraph>

              <Form
                form={judgerForm}
                layout="vertical"
                initialValues={{
                  judgerName: 'default-judger',
                  maxConcurrentTasks: 4,
                }}
              >
                <Row gutter={16}>
                  <Col span={12}>
                    <Form.Item
                      name="judgerName"
                      label="Judger 名称"
                      rules={[
                        { required: true, message: '请输入 Judger 名称' },
                        { pattern: /^[a-zA-Z0-9_-]+$/, message: '只能包含字母、数字、下划线和横线' },
                      ]}
                    >
                      <Input placeholder="default-judger" />
                    </Form.Item>
                  </Col>
                  <Col span={12}>
                    <Form.Item
                      name="maxConcurrentTasks"
                      label="最大并发数"
                      tooltip="同时处理的最大判题任务数"
                    >
                      <InputNumber min={1} max={32} className="w-full" />
                    </Form.Item>
                  </Col>
                </Row>

                <Form.Item
                  name="judgerDescription"
                  label="描述"
                >
                  <Input.TextArea rows={2} placeholder="可选，描述此 Judger 节点的用途" />
                </Form.Item>

                <Alert
                  type="info"
                  showIcon
                  message="初始化完成后，您将获得此 Judger 节点的连接密钥"
                  description="请妥善保管密钥，用于在 Judger 配置文件中设置身份认证。"
                />
              </Form>
            </div>
          )}

          {/* Step 3: 系统配置 */}
          {currentStep === 3 && (
            <div>
              <Title level={4}>
                <SettingOutlined className="mr-2" />
                系统配置
              </Title>
              <Paragraph type="secondary" className="mb-6">
                配置站点信息和其他系统设置。这些配置可以在管理后台修改。
              </Paragraph>

              <Form
                form={configForm}
                layout="vertical"
                initialValues={{
                  siteName: 'Aurora Judge',
                  siteDescription: '一个现代化的在线评测系统',
                  allowRegistration: true,
                  serverPort: 5000,
                  useHttps: false,
                  corsOrigins: 'http://localhost:3000',
                  redisEnabled: false,
                  storageType: 'local',
                  localPath: './data',
                }}
              >
                {/* 站点配置 */}
                <Divider orientation="left">站点信息</Divider>
                <Row gutter={16}>
                  <Col span={12}>
                    <Form.Item
                      name="siteName"
                      label="站点名称"
                      rules={[{ required: true, message: '请输入站点名称' }]}
                    >
                      <Input placeholder="Aurora Judge" />
                    </Form.Item>
                  </Col>
                  <Col span={12}>
                    <Form.Item
                      name="allowRegistration"
                      label="允许注册"
                      valuePropName="checked"
                    >
                      <Switch />
                    </Form.Item>
                  </Col>
                </Row>

                <Form.Item
                  name="siteDescription"
                  label="站点描述"
                >
                  <Input.TextArea rows={2} placeholder="一个现代化的在线评测系统" />
                </Form.Item>

                {/* 高级配置 */}
                <Collapse ghost className="mt-4">
                  <Panel header="高级配置" key="advanced">
                    {/* 服务器配置 */}
                    <Divider orientation="left" plain>服务器</Divider>
                    <Row gutter={16}>
                      <Col span={8}>
                        <Form.Item name="serverPort" label="端口">
                          <InputNumber min={1} max={65535} className="w-full" />
                        </Form.Item>
                      </Col>
                      <Col span={8}>
                        <Form.Item name="useHttps" label="启用 HTTPS" valuePropName="checked">
                          <Switch />
                        </Form.Item>
                      </Col>
                    </Row>

                    <Form.Item
                      name="corsOrigins"
                      label="CORS 允许的域名"
                      tooltip="多个域名用逗号分隔"
                    >
                      <Input placeholder="http://localhost:3000,https://example.com" />
                    </Form.Item>

                    {/* Redis 配置 */}
                    <Divider orientation="left" plain>Redis（可选）</Divider>
                    <Row gutter={16}>
                      <Col span={6}>
                        <Form.Item name="redisEnabled" label="启用 Redis" valuePropName="checked">
                          <Switch />
                        </Form.Item>
                      </Col>
                      <Col span={18}>
                        <Form.Item
                          noStyle
                          shouldUpdate={(prev, cur) => prev.redisEnabled !== cur.redisEnabled}
                        >
                          {({ getFieldValue }) => 
                            getFieldValue('redisEnabled') && (
                              <Form.Item name="redisConnection" label="Redis 连接字符串">
                                <Input placeholder="localhost:6379" />
                              </Form.Item>
                            )
                          }
                        </Form.Item>
                      </Col>
                    </Row>

                    {/* 存储配置 */}
                    <Divider orientation="left" plain>文件存储</Divider>
                    <Row gutter={16}>
                      <Col span={8}>
                        <Form.Item name="storageType" label="存储类型">
                          <Select>
                            <Select.Option value="local">本地存储</Select.Option>
                            <Select.Option value="minio">MinIO</Select.Option>
                          </Select>
                        </Form.Item>
                      </Col>
                      <Col span={16}>
                        <Form.Item
                          noStyle
                          shouldUpdate={(prev, cur) => prev.storageType !== cur.storageType}
                        >
                          {({ getFieldValue }) => 
                            getFieldValue('storageType') === 'local' ? (
                              <Form.Item name="localPath" label="存储路径">
                                <Input placeholder="./data" />
                              </Form.Item>
                            ) : (
                              <>
                                <Form.Item name="minioEndpoint" label="MinIO 地址">
                                  <Input placeholder="localhost:9000" />
                                </Form.Item>
                                <Row gutter={16}>
                                  <Col span={12}>
                                    <Form.Item name="minioAccessKey" label="Access Key">
                                      <Input />
                                    </Form.Item>
                                  </Col>
                                  <Col span={12}>
                                    <Form.Item name="minioSecretKey" label="Secret Key">
                                      <Password />
                                    </Form.Item>
                                  </Col>
                                </Row>
                                <Row gutter={16}>
                                  <Col span={12}>
                                    <Form.Item name="minioBucket" label="Bucket">
                                      <Input placeholder="aurora-judge" />
                                    </Form.Item>
                                  </Col>
                                  <Col span={12}>
                                    <Form.Item name="minioUseSSL" label="使用 SSL" valuePropName="checked">
                                      <Switch />
                                    </Form.Item>
                                  </Col>
                                </Row>
                              </>
                            )
                          }
                        </Form.Item>
                      </Col>
                    </Row>
                  </Panel>
                </Collapse>
              </Form>
            </div>
          )}

          {/* 操作按钮 */}
          <Divider />
          <div className="flex justify-between">
            <Button 
              disabled={currentStep === 0}
              onClick={handlePrev}
            >
              上一步
            </Button>
            <Space>
              {currentStep < 3 && (
                <Button type="primary" onClick={handleNext}>
                  下一步
                </Button>
              )}
              {currentStep === 3 && (
                <Button 
                  type="primary" 
                  onClick={handleInitialize}
                  loading={initializing}
                  icon={initializing ? <LoadingOutlined /> : <CheckCircleOutlined />}
                >
                  {initializing ? '正在初始化...' : '完成设置'}
                </Button>
              )}
            </Space>
          </div>
        </Card>

        {/* 底部提示 */}
        <div className="text-center mt-6 text-gray-500">
          <Text type="secondary">
            Aurora Judge - 一个现代化的在线评测系统
          </Text>
        </div>
      </div>
    </div>
  )
}

export default SetupPage

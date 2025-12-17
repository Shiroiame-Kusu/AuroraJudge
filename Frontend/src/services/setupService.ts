import axios from 'axios'

// 使用独立的 axios 实例，不带认证
const setupApi = axios.create({
  baseURL: '/api',
  timeout: 60000, // 初始化可能需要较长时间
  headers: {
    'Content-Type': 'application/json',
  },
})

setupApi.interceptors.response.use(
  (response) => response.data,
  (error) => {
    const message = error.response?.data?.message || error.message || '请求失败'
    return Promise.reject(new Error(message))
  }
)

// ================== 类型定义 ==================

export interface SetupStatus {
  needsSetup: boolean
  message: string
  currentConfig?: SetupCurrentConfig
}

export interface SetupCurrentConfig {
  databaseHost: string
  databasePort: number
  databaseName: string
  databaseUser: string
  storageType: string
  storagePath: string
  judgeMode: string
}

// 用于前端表单的配置接口（不同于后端返回的 currentConfig）
export interface DatabaseConfig {
  host: string
  port: number
  database: string
  username: string
  password: string
}

export interface AdminConfig {
  username: string
  email: string
  password: string
  displayName?: string
}

export interface JudgerConfig {
  name?: string
  description?: string
  maxConcurrentTasks?: number
}

export interface SecurityConfig {
  jwtSecret?: string
  accessTokenExpirationMinutes?: number
  refreshTokenExpirationDays?: number
}

export interface RedisConfig {
  connection?: string
}

export interface StorageConfig {
  type?: 'Local' | 'Minio'
  localPath?: string
  minio?: MinioConfig
}

export interface MinioConfig {
  endpoint?: string
  accessKey?: string
  secretKey?: string
  bucket?: string
  useSsl?: boolean
}

export interface CorsConfig {
  origins?: string
}

export interface ServerConfig {
  environment?: string
  urls?: string
}

export interface SiteConfig {
  name?: string
  description?: string
  allowRegister?: boolean
}

export interface SetupRequest {
  database: DatabaseConfig
  admin: AdminConfig
  judger?: JudgerConfig
  security?: SecurityConfig
  redis?: RedisConfig
  storage?: StorageConfig
  cors?: CorsConfig
  server?: ServerConfig
  site?: SiteConfig
}

export interface SetupResponse {
  success: boolean
  message: string
  judgerCredentials?: {
    name: string
    secret: string
  }
}

// ================== API 调用 ==================

export const setupService = {
  /**
   * 获取系统设置状态
   */
  getStatus: async (): Promise<SetupStatus> => {
    return setupApi.get('/setup/status')
  },

  /**
   * 测试数据库连接
   */
  testDatabase: async (config: DatabaseConfig): Promise<{ success: boolean; message: string }> => {
    return setupApi.post('/setup/test-database', config)
  },

  /**
   * 执行系统初始化
   */
  initialize: async (request: SetupRequest): Promise<SetupResponse> => {
    return setupApi.post('/setup/initialize', request)
  },
}

export default setupService

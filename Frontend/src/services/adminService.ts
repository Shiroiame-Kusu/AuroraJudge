import api, { ApiResponse, PagedResult } from './api'

export interface AdminUser {
  id: string
  username: string
  email: string
  avatar?: string | null
  bio?: string | null
  realName?: string | null
  organization?: string | null
  status: number
  solvedCount: number
  submissionCount: number
  rating: number
  maxRating: number
  createdAt: string
  lastLoginAt?: string | null
  roles?: string[]
  permissions?: string[]
}

export interface RoleDto {
  id: string
  code: string
  name: string
  description?: string | null
  isSystem: boolean
  priority: number
  createdAt: string
  permissionCodes?: string[]
}

export interface PermissionDto {
  id: string
  code: string
  name: string
  category?: string | null
  order: number
}

export interface SystemConfigDto {
  id: string
  key: string
  value: string
  type: string
  category?: string | null
  description?: string | null
  isPublic: boolean
  updatedAt: string
}

export interface LanguageConfigDto {
  id: string
  code: string
  name: string
  sourceFileName: string
  executableFileName?: string | null
  compileCommand?: string | null
  runCommand?: string | null
  timeMultiplier: number
  memoryMultiplier: number
  isEnabled: boolean
  order: number
}

export interface JudgerStatusDto {
  id: string
  judgerId: string
  name: string
  hostname?: string | null
  isOnline: boolean
  isEnabled: boolean
  currentTasks: number
  maxTasks: number
  cpuUsage?: number | null
  memoryUsage?: number | null
  completedTasks: number
  version?: string | null
  lastHeartbeat?: string | null
  startedAt?: string | null
}

export interface GenerateJudgerConfigRequest {
  name: string
  maxConcurrentTasks: number
  supportedLanguages?: string[]
  mode?: 'http' | 'rabbitmq'
  backendUrl?: string
  pollIntervalMs?: number
  workDir?: string
  rabbitMqConnection?: string
  logLevel?: string
}

export interface GenerateJudgerConfigResponse {
  judgerId: string
  name: string
  secret: string
  mode: 'http' | 'rabbitmq'
  backendUrl: string
  configText: string
}

export interface JudgerRuntimeNodeInfo {
  id: string
  name: string
  maxConcurrentTasks: number
  currentTasks: number
  status: string
  lastHeartbeat?: string | null
  supportedLanguages: string[]
}

export interface JudgerRuntimeStatusResponse {
  pendingTasks: number
  judgers: JudgerRuntimeNodeInfo[]
}

export interface AuditLogDto {
  id: string
  userId?: string | null
  username?: string | null
  action: number
  entityType?: string | null
  entityId?: string | null
  description: string
  ipAddress?: string | null
  timestamp: string
}

export interface CreateRoleRequest {
  name: string
  code: string
  description?: string
  priority?: number
}

export interface UpdateRoleRequest {
  name: string
  code: string
  description?: string
  priority?: number
}

export interface AuditLogQuery {
  page?: number
  pageSize?: number
}

export const adminService = {
  getUsers: (query: { page?: number; pageSize?: number; search?: string }): Promise<ApiResponse<PagedResult<AdminUser>>> =>
    api.get('/admin/users', { params: query }),

  banUser: (id: string): Promise<ApiResponse> => api.post(`/admin/users/${id}/ban`),
  unbanUser: (id: string): Promise<ApiResponse> => api.post(`/admin/users/${id}/unban`),

  getRoles: (): Promise<ApiResponse<RoleDto[]>> => api.get('/admin/roles'),
  createRole: (data: CreateRoleRequest): Promise<ApiResponse<RoleDto>> => api.post('/admin/roles', data),
  updateRole: (id: string, data: UpdateRoleRequest): Promise<ApiResponse<RoleDto>> => api.put(`/admin/roles/${id}`, data),
  deleteRole: (id: string): Promise<ApiResponse> => api.delete(`/admin/roles/${id}`),

  getPermissions: (): Promise<ApiResponse<PermissionDto[]>> => api.get('/admin/permissions'),

  getSettings: (): Promise<ApiResponse<SystemConfigDto[]>> => api.get('/admin/settings'),
  updateSetting: (key: string, value: string): Promise<ApiResponse> => api.put(`/admin/settings/${encodeURIComponent(key)}`, { value }),

  getLanguages: (): Promise<ApiResponse<LanguageConfigDto[]>> => api.get('/admin/languages'),

  getJudgers: (): Promise<ApiResponse<JudgerStatusDto[]>> => api.get('/admin/judgers'),
  setJudgerEnabled: (id: string, enabled: boolean): Promise<ApiResponse> =>
    api.put(`/admin/judgers/${id}/enabled`, undefined, { params: { enabled } }),

  generateJudgerConfig: (data: GenerateJudgerConfigRequest): Promise<ApiResponse<GenerateJudgerConfigResponse>> =>
    api.post('/admin/judgers/config', data),
  getJudgerRuntimeStatus: (): Promise<ApiResponse<JudgerRuntimeStatusResponse>> => api.get('/admin/judgers/runtime-status'),

  getAuditLogs: (query: AuditLogQuery): Promise<ApiResponse<PagedResult<AuditLogDto>>> =>
    api.get('/admin/audit-logs', { params: query }),
}

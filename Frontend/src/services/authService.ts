import api, { ApiResponse } from './api'
import { User } from '@/stores'

export interface LoginRequest {
  usernameOrEmail: string
  password: string
  rememberMe?: boolean
}

export interface RegisterRequest {
  username: string
  email: string
  password: string
  confirmPassword: string
}

export interface LoginResponse {
  user: User
  accessToken: string
  refreshToken: string
  expiresAt: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

export const authService = {
  login: (data: LoginRequest): Promise<ApiResponse<LoginResponse>> => 
    api.post('/auth/login', data),
  
  register: (data: RegisterRequest): Promise<ApiResponse> => 
    api.post('/auth/register', data),
  
  refresh: (refreshToken: string): Promise<ApiResponse<LoginResponse>> => 
    api.post('/auth/refresh', { refreshToken }),
  
  logout: (): Promise<ApiResponse> => 
    api.post('/auth/logout'),
  
  getCurrentUser: (): Promise<ApiResponse<User>> => 
    api.get('/auth/profile'),
  
  changePassword: (data: ChangePasswordRequest): Promise<ApiResponse> => 
    api.post('/auth/change-password', data),
}

import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export interface User {
  id: string
  username: string
  email: string
  nickname?: string
  avatar?: string
  roles?: string[]
  permissions?: string[]
}

interface AuthState {
  user: User | null
  accessToken: string | null
  refreshToken: string | null
  isAuthenticated: boolean
  
  setAuth: (user: User, accessToken: string, refreshToken: string) => void
  clearAuth: () => void
  updateUser: (user: Partial<User>) => void
  hasPermission: (permission: string) => boolean
  hasRole: (role: string) => boolean
  isAdmin: () => boolean
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,
      
      setAuth: (user, accessToken, refreshToken) => set({
        user: {
          ...user,
          roles: user.roles ?? [],
          permissions: user.permissions ?? [],
        },
        accessToken,
        refreshToken,
        isAuthenticated: true,
      }),
      
      clearAuth: () => set({
        user: null,
        accessToken: null,
        refreshToken: null,
        isAuthenticated: false,
      }),
      
      updateUser: (userData) => set((state) => {
        if (!state.user) return { user: null }

        const nextUser = { ...state.user, ...userData }
        return {
          user: {
            ...nextUser,
            roles: nextUser.roles ?? [],
            permissions: nextUser.permissions ?? [],
          },
        }
      }),
      
      hasPermission: (permission) => {
        const { user } = get()
        if (!user) return false
        const permissions = user.permissions ?? []
        if (permissions.includes('*')) return true
        return permissions.includes(permission)
      },
      
      hasRole: (role) => {
        const { user } = get()
        if (!user) return false
        return (user.roles ?? []).includes(role)
      },
      
      isAdmin: () => {
        const { user } = get()
        if (!user) return false
        const roles = (user.roles ?? []).map((r) => r.toLowerCase())
        const permissions = user.permissions ?? []
        return roles.includes('admin') || roles.includes('super_admin') || permissions.includes('*')
      },
    }),
    {
      name: 'aurora-auth',
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        user: state.user,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
)

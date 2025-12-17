import api, { ApiResponse, PagedResult } from './api'

// Align with backend enums in `AuroraJudge.Domain.Enums.Enums.cs`
export enum ContestStatus {
  Pending = 0,
  Running = 1,
  Frozen = 2,
  Ended = 3,
}

export enum ContestType {
  ACM = 0,
  OI = 1,
  IOI = 2,
  LeDuo = 3,
  Homework = 4,
}

export enum ContestVisibility {
  Public = 0,
  Protected = 1,
  Private = 2,
}

export interface Contest {
  id: string
  title: string
  description?: string | null
  startTime: string
  endTime: string
  type: ContestType
  visibility: ContestVisibility
  isRated: boolean
  status: ContestStatus
  participantCount: number
  problemCount: number
  creatorId: string
  creatorName: string
  createdAt: string
}

export interface ContestDetail extends Contest {
  freezeTime?: string | null
  ratingFloor?: number | null
  ratingCeiling?: number | null
  allowLateSubmission: boolean
  lateSubmissionPenalty: number
  showRanking: boolean
  allowViewOthersCode: boolean
  publishProblemsAfterEnd: boolean
  maxParticipants?: number | null
  rules?: string | null
  problems: ContestProblem[]
  isRegistered: boolean
}

export interface ContestProblem {
  problemId: string
  label: string
  title: string
  score?: number | null
  color?: string | null
  acceptedCount: number
  submissionCount: number
  solved?: boolean | null
}

export interface ContestRanking {
  rank: number
  userId: string
  username: string
  avatar?: string | null
  score: number
  penalty: number
  solvedCount: number
  problems: ContestProblemResult[]
}

export interface ContestProblemResult {
  label: string
  solved: boolean
  firstBlood: boolean
  attempts: number
  solvedTime?: number | null
  score?: number | null
}

export interface ContestAnnouncement {
  id: string
  title: string
  content: string
  isPinned: boolean
  problemId?: string | null
  problemLabel?: string | null
  createdAt: string
}

export interface ContestQuery {
  page?: number
  pageSize?: number
  status?: number  // 后端使用 int
  type?: number    // 后端使用 int
}

export interface CreateContestRequest {
  title: string
  description?: string | null
  startTime: string
  endTime: string
  freezeTime?: string | null
  type: ContestType
  visibility: ContestVisibility
  password?: string | null
  isRated: boolean
  ratingFloor?: number | null
  ratingCeiling?: number | null
  allowLateSubmission: boolean
  lateSubmissionPenalty: number
  showRanking: boolean
  allowViewOthersCode: boolean
  publishProblemsAfterEnd: boolean
  maxParticipants?: number | null
  rules?: string | null
  problems?: ContestProblemRequest[] | null
}

export interface ContestProblemRequest {
  problemId: string
  label: string
  order: number
  score?: number | null
  color?: string | null
}

export interface UpdateContestRequest {
  title: string
  description?: string | null
  startTime: string
  endTime: string
  freezeTime?: string | null
  type: ContestType
  visibility: ContestVisibility
  password?: string | null
  isRated: boolean
  ratingFloor?: number | null
  ratingCeiling?: number | null
  allowLateSubmission: boolean
  lateSubmissionPenalty: number
  showRanking: boolean
  allowViewOthersCode: boolean
  publishProblemsAfterEnd: boolean
  maxParticipants?: number | null
  rules?: string | null
}

export const contestService = {
  getList: (query: ContestQuery): Promise<ApiResponse<PagedResult<Contest>>> => 
    api.get('/contests', { params: query }),
  
  getById: (id: string): Promise<ApiResponse<ContestDetail>> => 
    api.get(`/contests/${id}`),
  
  register: (id: string, password?: string): Promise<ApiResponse> => 
    api.post(`/contests/${id}/register`, { password }),
  
  getRanking: (id: string): Promise<ApiResponse<ContestRanking[]>> => 
    api.get(`/contests/${id}/standings`),
  
  unregister: (id: string): Promise<ApiResponse> => 
    api.delete(`/contests/${id}/register`),
  
  getProblems: (id: string): Promise<ApiResponse<ContestProblem[]>> =>
    api.get(`/contests/${id}/problems`),
  
  getAnnouncements: (id: string): Promise<ApiResponse<ContestAnnouncement[]>> =>
    api.get(`/contests/${id}/announcements`),
  
  // 管理员接口
  create: (data: CreateContestRequest): Promise<ApiResponse<Contest>> => 
    api.post('/contests', data),
  
  update: (id: string, data: UpdateContestRequest): Promise<ApiResponse<Contest>> => 
    api.put(`/contests/${id}`, data),
  
  delete: (id: string): Promise<ApiResponse> => 
    api.delete(`/contests/${id}`),
}

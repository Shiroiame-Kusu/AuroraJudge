import api, { ApiResponse, PagedResult } from './api'

// Align with backend `AuroraJudge.Domain.Enums.JudgeStatus` (serialized as number)
export enum JudgeStatus {
  Pending = 0,
  Judging = 1,
  Compiling = 2,
  Running = 3,
  Accepted = 10,
  WrongAnswer = 11,
  TimeLimitExceeded = 12,
  MemoryLimitExceeded = 13,
  OutputLimitExceeded = 14,
  RuntimeError = 15,
  CompileError = 16,
  PresentationError = 17,
  SystemError = 20,
  PartiallyAccepted = 21,
  Skipped = 22,
}

export interface Submission {
  id: string
  problemId: string
  problemTitle: string
  userId: string
  username: string
  contestId?: string | null
  contestTitle?: string | null
  language: string
  codeLength: number
  status: JudgeStatus
  score?: number
  timeUsed?: number
  memoryUsed?: number
  submittedAt: string
  judgedAt?: string | null
}

export interface SubmissionDetail extends Submission {
  code: string
  compileMessage?: string | null
  judgeMessage?: string | null
  results: JudgeResult[]
}

export interface SubmissionSimilarity {
  submissionId: string
  userId: string
  username: string
  problemId: string
  problemTitle: string
  language: string
  codeLength: number
  submittedAt: string
  similarity: number // 0-100
}

export interface JudgeResult {
  testCaseOrder: number
  subtask?: number | null
  status: JudgeStatus
  timeUsed: number
  memoryUsed: number
  score: number
  message?: string | null
}

export interface SubmitRequest {
  problemId: string
  language: string
  code: string
  contestId?: string
}

export interface SubmissionQuery {
  page?: number
  pageSize?: number
  problemId?: string
  userId?: string
  language?: string
  status?: number
  contestId?: string
  username?: string
}

export const submissionService = {
  getList: (query: SubmissionQuery): Promise<ApiResponse<PagedResult<Submission>>> => 
    api.get('/submissions', { params: query }),
  
  getById: (id: string): Promise<ApiResponse<SubmissionDetail>> => 
    api.get(`/submissions/${id}`),
  
  submit: (data: SubmitRequest): Promise<ApiResponse<Submission>> => 
    api.post('/submissions', data),
  
  getMy: (query: SubmissionQuery): Promise<ApiResponse<PagedResult<Submission>>> => 
    api.get('/submissions/my', { params: query }),
  
  rejudge: (id: string): Promise<ApiResponse> => 
    api.post(`/submissions/${id}/rejudge`),

  getSimilarity: (
    id: string,
    params?: { top?: number; candidateLimit?: number }
  ): Promise<ApiResponse<SubmissionSimilarity[]>> =>
    api.get(`/submissions/${id}/similarity`, { params }),
}

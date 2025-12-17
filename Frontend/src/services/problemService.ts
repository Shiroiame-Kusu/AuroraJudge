import axios from 'axios'
import api, { ApiResponse, PagedResult } from './api'
import { useAuthStore } from '@/stores'

export interface Tag {
  id: string
  name: string
  color?: string
  category?: string
  usageCount: number
}

export interface CreateTagRequest {
  name: string
  color?: string | null
  category?: string | null
}

export interface Problem {
  id: string
  title: string
  difficulty: number  // 后端使用枚举数值
  acceptedCount: number
  submissionCount: number
  tags: Tag[]
  solved?: boolean  // null 表示未登录
}

export interface ProblemDetail extends Problem {
  description: string
  inputFormat: string
  outputFormat: string
  sampleInput?: string
  sampleOutput?: string
  hint?: string
  source?: string
  timeLimit: number
  memoryLimit: number
  stackLimit: number
  outputLimit: number
  judgeMode: number
  specialJudgeCode?: string | null
  specialJudgeLanguage?: string | null
  allowedLanguages?: string | null
  visibility: number
  acceptRate: number
  createdAt: string
}

export interface CreateProblemRequest {
  title: string
  description: string
  inputFormat: string
  outputFormat: string
  sampleInput?: string | null
  sampleOutput?: string | null
  hint?: string | null
  source?: string | null
  timeLimit: number
  memoryLimit: number
  stackLimit: number
  outputLimit: number
  judgeMode: number
  specialJudgeCode?: string | null
  specialJudgeLanguage?: string | null
  allowedLanguages?: string | null
  visibility: number
  difficulty: number
  tagIds?: string[] | null
}

export type UpdateProblemRequest = CreateProblemRequest

export interface TestCase {
  id: string
  order: number
  inputSize: number
  outputSize: number
  score: number
  isSample: boolean
  subtask?: number | null
  description?: string | null
}

export interface CreateTestCaseRequest {
  order: number
  score: number
  isSample: boolean
  subtask?: number | null
  description?: string | null
}

export type TestCaseFileType = 'input' | 'output'

export interface ProblemQuery {
  page?: number
  pageSize?: number
  search?: string  // 后端使用 search 而非 keyword
  tagId?: string
  difficulty?: number  // 后端使用 int
}

export const problemService = {
  getList: (query: ProblemQuery): Promise<ApiResponse<PagedResult<Problem>>> => 
    api.get('/problems', { params: query }),
  
  getById: (id: string, params?: { contestId?: string }): Promise<ApiResponse<ProblemDetail>> => 
    api.get(`/problems/${id}`, { params }),
  
  getTags: (): Promise<ApiResponse<Tag[]>> => 
    api.get('/problems/tags'),

  createTag: (data: CreateTagRequest): Promise<ApiResponse<Tag>> =>
    api.post('/problems/tags', data),

  updateTag: (id: string, data: CreateTagRequest): Promise<ApiResponse<Tag>> =>
    api.put(`/problems/tags/${id}`, data),

  deleteTag: (id: string): Promise<ApiResponse> =>
    api.delete(`/problems/tags/${id}`),
  
  // 管理员接口
  create: (data: CreateProblemRequest): Promise<ApiResponse<ProblemDetail>> => 
    api.post('/problems', data),
  
  update: (id: string, data: UpdateProblemRequest): Promise<ApiResponse<ProblemDetail>> => 
    api.put(`/problems/${id}`, data),
  
  delete: (id: string): Promise<ApiResponse> => 
    api.delete(`/problems/${id}`),

  // 测试用例管理（管理员权限）
  getTestCases: (problemId: string): Promise<ApiResponse<TestCase[]>> =>
    api.get(`/problems/${problemId}/testcases`),

  uploadTestCase: (
    problemId: string,
    request: CreateTestCaseRequest,
    inputFile: File,
    outputFile: File
  ): Promise<ApiResponse<TestCase>> => {
    const formData = new FormData()
    formData.append('order', String(request.order))
    formData.append('score', String(request.score))
    formData.append('isSample', String(request.isSample))
    if (request.subtask !== undefined && request.subtask !== null) {
      formData.append('subtask', String(request.subtask))
    }
    if (request.description) {
      formData.append('description', request.description)
    }
    formData.append('inputFile', inputFile)
    formData.append('outputFile', outputFile)

    return api.post(`/problems/${problemId}/testcases`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  deleteTestCase: (problemId: string, testCaseId: string): Promise<ApiResponse> =>
    api.delete(`/problems/${problemId}/testcases/${testCaseId}`),

  downloadTestCaseFile: async (
    problemId: string,
    testCaseId: string,
    type: TestCaseFileType
  ): Promise<{ blob: Blob; filename: string }> => {
    const { accessToken } = useAuthStore.getState()
    const resp = await axios.get(`/api/problems/${problemId}/testcases/${testCaseId}/download`, {
      params: { type },
      responseType: 'blob',
      headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : undefined,
    })

    const disposition = resp.headers?.['content-disposition'] as string | undefined
    const match = disposition?.match(/filename\*=UTF-8''([^;]+)|filename="?([^";]+)"?/i)
    const encoded = match?.[1] ?? match?.[2]
    const filename = encoded ? decodeURIComponent(encoded) : `${type}_${testCaseId}.txt`

    return { blob: resp.data as Blob, filename }
  },

  rejudge: (problemId: string): Promise<ApiResponse> =>
    api.post(`/problems/${problemId}/rejudge`),
}

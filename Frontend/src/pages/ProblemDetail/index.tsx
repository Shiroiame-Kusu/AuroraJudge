import { useEffect, useMemo, useState } from 'react'
import { useParams, useNavigate, useSearchParams } from 'react-router-dom'
import { Card, Tabs, Tag, Space, Button, Select, message, Descriptions, Collapse } from 'antd'
import { SendOutlined, HistoryOutlined } from '@ant-design/icons'
import { useRequest } from 'ahooks'
import { problemService, submissionService, type ProblemDetail } from '@/services'
import { DifficultyTag, MarkdownRenderer, CodeEditor, LoadingSpinner } from '@/components'
import { useAuthStore } from '@/stores'

const languageOptions = [
  { value: 'cpp', label: 'C++' },
  { value: 'c', label: 'C' },
  { value: 'java', label: 'Java' },
  { value: 'python', label: 'Python' },
]

const defaultCode: Record<string, string> = {
  cpp: '#include <iostream>\nusing namespace std;\n\nint main() {\n    \n    return 0;\n}',
  c: '#include <stdio.h>\n\nint main() {\n    \n    return 0;\n}',
  java: 'public class Main {\n    public static void main(String[] args) {\n        \n    }\n}',
  python: '# Python solution\n',
}

const parseSampleField = (text?: string | null): string[] => {
  if (!text) return []
  const raw = String(text).trim()
  if (!raw) return []

  // Preferred format: JSON array of strings, e.g. ["1 2\n", "3 4\n"]
  if (raw.startsWith('[')) {
    try {
      const parsed = JSON.parse(raw)
      if (Array.isArray(parsed) && parsed.every((x) => typeof x === 'string')) {
        return parsed.map((s) => String(s))
      }
    } catch {
      // Fall through to legacy parsing
    }
  }

  // Legacy format fallback: split by a line containing only '---'
  return raw
    .split(/\r?\n\s*---\s*\r?\n/g)
    .map((s) => s.trimEnd())
    .filter((s) => s.length > 0)
}

const ProblemDetailPage = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const { isAuthenticated } = useAuthStore()

  const contestId = searchParams.get('contestId') || undefined
  
  const [language, setLanguage] = useState('cpp')
  const [code, setCode] = useState(defaultCode.cpp)
  const [submitting, setSubmitting] = useState(false)
  
  const { data, loading } = useRequest(
    () => problemService.getById(id!, contestId ? { contestId } : undefined),
    { ready: !!id }
  )
  
  const problem = data?.data as ProblemDetail | undefined

  const inputFormatText = useMemo(() => {
    const raw = problem?.inputFormat
    if (!raw) return ''
    return String(raw).replace(/\s+/g, ' ').trim()
  }, [problem?.inputFormat])

  const outputFormatText = useMemo(() => {
    const raw = problem?.outputFormat
    if (!raw) return ''
    return String(raw).replace(/\s+/g, ' ').trim()
  }, [problem?.outputFormat])

  const sampleInputs = useMemo(() => parseSampleField(problem?.sampleInput), [problem?.sampleInput])
  const sampleOutputs = useMemo(() => parseSampleField(problem?.sampleOutput), [problem?.sampleOutput])
  const samples = useMemo(() => {
    const count = Math.max(sampleInputs.length, sampleOutputs.length)
    return Array.from({ length: count }).map((_, i) => ({
      index: i + 1,
      input: sampleInputs[i],
      output: sampleOutputs[i],
    }))
  }, [sampleInputs, sampleOutputs])

  const allowedLanguageValues = useMemo(() => {
    const raw = problem?.allowedLanguages
    if (!raw) return null
    return raw
      .split(/[,;\s]+/g)
      .map((s) => s.trim())
      .filter(Boolean)
  }, [problem?.allowedLanguages])

  const effectiveLanguageOptions = useMemo(() => {
    if (!allowedLanguageValues) return languageOptions
    const known = languageOptions.filter((opt) => allowedLanguageValues.includes(opt.value))
    const knownValues = new Set(known.map((o) => o.value))
    const unknown = allowedLanguageValues
      .filter((v) => !knownValues.has(v))
      .map((v) => ({ value: v, label: v }))
    return [...known, ...unknown]
  }, [allowedLanguageValues])
  
  const handleLanguageChange = (value: string) => {
    setLanguage(value)
    setCode(defaultCode[value] || '')
  }

  // If backend restricts languages, ensure current selection is valid.
  useEffect(() => {
    if (!allowedLanguageValues?.length) return
    if (allowedLanguageValues.includes(language)) return
    const nextLang = allowedLanguageValues[0]
    if (!nextLang) return
    setLanguage(nextLang)
    setCode(defaultCode[nextLang] || '')
  }, [allowedLanguageValues, language])
  
  const handleSubmit = async () => {
    if (!isAuthenticated) {
      message.warning('请先登录')
      navigate('/login')
      return
    }
    
    if (!code.trim()) {
      message.warning('请输入代码')
      return
    }
    
    setSubmitting(true)
    try {
      const response = await submissionService.submit({
        problemId: id!,
        language,
        code,
        contestId,
      })
      
      if (response.success && response.data) {
        message.success('提交成功')
        navigate(`/submissions/${response.data.id}`)
      }
    } catch (error: any) {
      message.error(error.message || '提交失败')
    } finally {
      setSubmitting(false)
    }
  }
  
  if (loading) {
    return (
      <div className="flex items-center justify-center h-96">
        <LoadingSpinner size="large" />
      </div>
    )
  }
  
  if (!problem) {
    return <Card>题目不存在</Card>
  }
  
  return (
    <div className="flex gap-4 min-h-0" style={{ height: 'calc(100vh - 180px)' }}>
      {/* 左侧题目描述 */}
      <Card className="flex-1 overflow-auto min-h-0" style={{ height: '100%' }}>
        <div className="mb-4">
          <h1 className="text-2xl font-bold mb-2">{problem.title}</h1>
          <Space wrap>
            <DifficultyTag difficulty={problem.difficulty} />
            {problem.tags?.map((tag) => (
              <Tag key={tag.id} color={tag.color}>{tag.name}</Tag>
            ))}
          </Space>

          <Space wrap className="mt-2">
            <Tag className="aj-pill">时间限制: {problem.timeLimit} ms</Tag>
            <Tag className="aj-pill">内存限制: {(problem.memoryLimit / 1024).toFixed(0)} MB</Tag>
            <Tag className="aj-pill">栈限制: {(problem.stackLimit / 1024).toFixed(0)} MB</Tag>
            <Tag className="aj-pill">输出限制: {(problem.outputLimit / 1024).toFixed(0)} MB</Tag>
            <Tag className="aj-pill" title={inputFormatText || undefined}>
              输入格式: {inputFormatText || '-'}
            </Tag>
            <Tag className="aj-pill" title={outputFormatText || undefined}>
              输出格式: {outputFormatText || '-'}
            </Tag>
          </Space>
        </div>
        
        <Tabs
          items={[
            {
              key: 'description',
              label: '题目描述',
              children: (
                <div className="space-y-6">
                  <div>
                    <h3
                      className="mb-2"
                      style={{ fontSize: 20, fontWeight: 800, lineHeight: 1.2, fontSynthesis: 'weight' }}
                    >
                      描述
                    </h3>
                    <MarkdownRenderer content={problem.description} />
                  </div>

                  {problem.source && (
                    <Descriptions bordered column={1} size="small">
                      <Descriptions.Item label="来源">{problem.source}</Descriptions.Item>
                    </Descriptions>
                  )}

                  {allowedLanguageValues?.length ? (
                    <Descriptions bordered column={1} size="small">
                      <Descriptions.Item label="允许语言">{allowedLanguageValues.join(' / ')}</Descriptions.Item>
                    </Descriptions>
                  ) : null}
                  
                  <div>
                    <h3
                      className="mb-2"
                      style={{ fontSize: 20, fontWeight: 800, lineHeight: 1.2, fontSynthesis: 'weight' }}
                    >
                      样例
                    </h3>
                    {samples.length > 0 && (
                      <Collapse
                        items={samples.map((s) => ({
                          key: s.index,
                          label: `样例 ${s.index}`,
                          children: (
                            <div className="space-y-2">
                              {s.input && (
                                <div>
                                  <strong>输入：</strong>
                                  <pre className="bg-gray-100 p-2 rounded mt-1">{s.input}</pre>
                                </div>
                              )}
                              {s.output && (
                                <div>
                                  <strong>输出：</strong>
                                  <pre className="bg-gray-100 p-2 rounded mt-1">{s.output}</pre>
                                </div>
                              )}
                            </div>
                          ),
                        }))}
                        defaultActiveKey={samples.length ? [samples[0].index] : undefined}
                      />
                    )}
                  </div>
                  
                  {problem.hint && (
                    <div>
                      <h3
                        className="mb-2"
                        style={{ fontSize: 20, fontWeight: 800, lineHeight: 1.2, fontSynthesis: 'weight' }}
                      >
                        提示
                      </h3>
                      <MarkdownRenderer content={problem.hint} />
                    </div>
                  )}
                </div>
              ),
            },
            {
              key: 'submissions',
              label: '提交记录',
              children: (
                <Button
                  icon={<HistoryOutlined />}
                  onClick={() => navigate(`/submissions?problemId=${id}`)}
                >
                  查看所有提交
                </Button>
              ),
            },
          ]}
        />
      </Card>
      
      {/* 右侧代码编辑器 */}
      <Card
        className="flex-1 min-h-0"
        style={{ height: '100%' }}
        styles={{ body: { height: '100%', display: 'flex', flexDirection: 'column' } }}
      >
        <div className="flex justify-between items-center mb-4">
          <Select
            value={language}
            onChange={handleLanguageChange}
            options={effectiveLanguageOptions}
            style={{ width: 120 }}
          />
          <Space>
            <Button
              type="primary"
              icon={<SendOutlined />}
              loading={submitting}
              onClick={handleSubmit}
            >
              提交
            </Button>
          </Space>
        </div>
        
        <div className="flex-1 min-h-0" style={{ flex: 1 }}>
          <CodeEditor
            value={code}
            onChange={setCode}
            language={language}
            height="100%"
          />
        </div>
      </Card>
    </div>
  )
}

export default ProblemDetailPage

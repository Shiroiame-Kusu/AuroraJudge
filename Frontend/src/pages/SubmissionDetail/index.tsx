import { useParams, Link } from 'react-router-dom'
import { Card, Descriptions, Table, Alert, Tag } from 'antd'
import { useRequest } from 'ahooks'
import { submissionService, JudgeStatus, type SubmissionDetail, type JudgeResult } from '@/services'
import { StatusTag, CodeEditor, LoadingSpinner } from '@/components'
import type { ColumnsType } from 'antd/es/table'
import dayjs from 'dayjs'

const SubmissionDetailPage = () => {
  const { id } = useParams<{ id: string }>()
  
  const { data, loading } = useRequest(
    () => submissionService.getById(id!),
    { 
      ready: !!id,
      pollingInterval: 2000,
      pollingWhenHidden: false,
      pollingErrorRetryCount: 3,
    }
  )
  
  const submission = data?.data as SubmissionDetail | undefined
  
  // 停止轮询条件：不在进行中的状态
  const isInProgress = (status: JudgeStatus) =>
    status === JudgeStatus.Pending ||
    status === JudgeStatus.Judging ||
    status === JudgeStatus.Compiling ||
    status === JudgeStatus.Running

  const shouldStopPolling = submission ? !isInProgress(submission.status) : false
  
  const testCaseColumns: ColumnsType<JudgeResult> = [
    {
      title: '#',
      dataIndex: 'testCaseOrder',
      width: 60,
    },
    {
      title: '状态',
      dataIndex: 'status',
      width: 120,
      render: (status) => <StatusTag status={status} />,
    },
    {
      title: '用时',
      dataIndex: 'timeUsed',
      width: 100,
      render: (time) => `${time} ms`,
    },
    {
      title: '内存',
      dataIndex: 'memoryUsed',
      width: 100,
      render: (memory) => `${memory} KB`,
    },
    {
      title: '得分',
      dataIndex: 'score',
      width: 80,
    },
    {
      title: '信息',
      dataIndex: 'message',
      ellipsis: true,
    },
  ]
  
  if (loading && !submission) {
    return (
      <div className="flex items-center justify-center h-96">
        <LoadingSpinner size="large" />
      </div>
    )
  }
  
  if (!submission) {
    return <Card>提交记录不存在</Card>
  }
  
  return (
    <div className="space-y-4">
      <Card title="提交详情">
        <Descriptions bordered column={2}>
          <Descriptions.Item label="提交 ID">{submission.id}</Descriptions.Item>
          <Descriptions.Item label="题目">
            <Link to={`/problems/${submission.problemId}`}>{submission.problemTitle}</Link>
          </Descriptions.Item>
          <Descriptions.Item label="用户">{submission.username}</Descriptions.Item>
          <Descriptions.Item label="语言">
            <Tag>{submission.language}</Tag>
          </Descriptions.Item>
          <Descriptions.Item label="状态">
            <StatusTag status={submission.status} />
            {!shouldStopPolling && <LoadingSpinner size="small" className="ml-2" />}
          </Descriptions.Item>
          <Descriptions.Item label="得分">{submission.score ?? '-'}</Descriptions.Item>
          <Descriptions.Item label="用时">{submission.timeUsed ? `${submission.timeUsed} ms` : '-'}</Descriptions.Item>
          <Descriptions.Item label="内存">{submission.memoryUsed ? `${submission.memoryUsed} KB` : '-'}</Descriptions.Item>
          <Descriptions.Item label="提交时间" span={2}>
            {dayjs(submission.submittedAt).format('YYYY-MM-DD HH:mm:ss')}
          </Descriptions.Item>
        </Descriptions>
      </Card>
      
      {submission.compileMessage && (
        <Card title="编译信息">
          <Alert
            type={submission.status === JudgeStatus.CompileError ? 'error' : 'info'}
            message={<pre className="whitespace-pre-wrap text-sm">{submission.compileMessage}</pre>}
          />
        </Card>
      )}
      
      {submission.results && submission.results.length > 0 && (
        <Card title="测试点结果">
          <Table
            columns={testCaseColumns}
            dataSource={submission.results}
            rowKey="testCaseOrder"
            pagination={false}
            size="small"
          />
        </Card>
      )}
      
      <Card title="提交代码">
        <CodeEditor
          value={submission.code}
          onChange={() => {}}
          language={submission.language}
          height="400px"
          readOnly
        />
      </Card>
    </div>
  )
}

export default SubmissionDetailPage

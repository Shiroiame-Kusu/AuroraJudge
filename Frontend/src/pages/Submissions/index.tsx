import { useState } from 'react'
import { Table, Card, Select, Space, Button } from 'antd'
import { ReloadOutlined } from '@ant-design/icons'
import { Link, useSearchParams } from 'react-router-dom'
import { useRequest } from 'ahooks'
import { submissionService, JudgeStatus, type Submission, type SubmissionQuery } from '@/services'
import { StatusTag } from '@/components'
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table'
import dayjs from 'dayjs'

const SubmissionsPage = () => {
  const [searchParams] = useSearchParams()
  const problemId = searchParams.get('problemId') || undefined
  
  const [query, setQuery] = useState<SubmissionQuery>({
    page: 1,
    pageSize: 20,
    problemId,
  })
  
  const { data, loading, refresh } = useRequest(
    () => submissionService.getList(query),
    { refreshDeps: [query] }
  )
  
  const columns: ColumnsType<Submission> = [
    {
      title: '提交时间',
      dataIndex: 'submittedAt',
      width: 180,
      render: (time) => dayjs(time).format('YYYY-MM-DD HH:mm:ss'),
    },
    {
      title: '题目',
      dataIndex: 'problemTitle',
      render: (text, record) => (
        <Link to={`/problems/${record.problemId}`}>{text}</Link>
      ),
    },
    {
      title: '用户',
      dataIndex: 'username',
      width: 120,
    },
    {
      title: '语言',
      dataIndex: 'language',
      width: 100,
    },
    {
      title: '结果',
      dataIndex: 'status',
      width: 120,
      render: (status) => <StatusTag status={status} />,
    },
    {
      title: '用时',
      dataIndex: 'timeUsed',
      width: 100,
      render: (time) => time ? `${time} ms` : '-',
    },
    {
      title: '内存',
      dataIndex: 'memoryUsed',
      width: 100,
      render: (memory) => memory ? `${memory} KB` : '-',
    },
    {
      title: '操作',
      key: 'action',
      width: 80,
      render: (_, record) => (
        <Link to={`/submissions/${record.id}`}>详情</Link>
      ),
    },
  ]
  
  const handleTableChange = (pagination: TablePaginationConfig) => {
    setQuery((prev) => ({
      ...prev,
      page: pagination.current,
      pageSize: pagination.pageSize,
    }))
  }
  
  return (
    <Card title="提交记录">
      <Space className="mb-4" wrap>
        <Select
          placeholder="语言"
          allowClear
          style={{ width: 120 }}
          onChange={(value) => setQuery((prev) => ({ ...prev, language: value, page: 1 }))}
          options={[
            { value: 'cpp', label: 'C++' },
            { value: 'c', label: 'C' },
            { value: 'java', label: 'Java' },
            { value: 'python', label: 'Python' },
          ]}
        />
        
        <Select
          placeholder="状态"
          allowClear
          style={{ width: 140 }}
          onChange={(value) => setQuery((prev) => ({ ...prev, status: value, page: 1 }))}
          options={[
            { value: JudgeStatus.Accepted, label: '通过' },
            { value: JudgeStatus.WrongAnswer, label: '答案错误' },
            { value: JudgeStatus.TimeLimitExceeded, label: '超时' },
            { value: JudgeStatus.MemoryLimitExceeded, label: '超内存' },
            { value: JudgeStatus.RuntimeError, label: '运行错误' },
            { value: JudgeStatus.CompileError, label: '编译错误' },
          ]}
        />
        
        <Button icon={<ReloadOutlined />} onClick={refresh}>
          刷新
        </Button>
      </Space>
      
      <Table
        columns={columns}
        dataSource={data?.data?.items || []}
        rowKey="id"
        loading={loading}
        pagination={{
          current: data?.data?.page || 1,
          pageSize: data?.data?.pageSize || 20,
          total: data?.data?.total || 0,
          showSizeChanger: true,
          showQuickJumper: true,
          showTotal: (total) => `共 ${total} 条记录`,
        }}
        onChange={handleTableChange}
      />
    </Card>
  )
}

export default SubmissionsPage

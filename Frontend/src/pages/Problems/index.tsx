import { useState } from 'react'
import { Table, Card, Input, Select, Space, Tag, Button } from 'antd'
import { SearchOutlined, ReloadOutlined } from '@ant-design/icons'
import { Link } from 'react-router-dom'
import { useRequest } from 'ahooks'
import { problemService, type Problem, type ProblemQuery, type Tag as TagType } from '@/services'
import { DifficultyTag } from '@/components'
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table'

const { Search } = Input

const ProblemsPage = () => {
  const [query, setQuery] = useState<ProblemQuery>({
    page: 1,
    pageSize: 20,
  })
  
  const { data, loading, refresh } = useRequest(
    () => problemService.getList(query),
    { refreshDeps: [query] }
  )
  
  const { data: tagsData } = useRequest(() => problemService.getTags())
  
  const columns: ColumnsType<Problem> = [
    {
      title: '#',
      dataIndex: 'id',
      width: 80,
      render: (_, __, index) => ((query.page || 1) - 1) * (query.pageSize || 20) + index + 1,
    },
    {
      title: '题目',
      dataIndex: 'title',
      render: (text, record) => (
        <Link to={`/problems/${record.id}`} className="text-blue-600 hover:text-blue-800">
          {text}
        </Link>
      ),
    },
    {
      title: '难度',
      dataIndex: 'difficulty',
      width: 100,
      render: (difficulty) => <DifficultyTag difficulty={difficulty} />,
    },
    {
      title: '标签',
      dataIndex: 'tags',
      width: 200,
      render: (tags: TagType[]) => (
        <Space wrap>
          {tags?.slice(0, 3).map((tag) => (
            <Tag key={tag.id} color={tag.color}>{tag.name}</Tag>
          ))}
        </Space>
      ),
    },
    {
      title: '通过率',
      key: 'acceptRate',
      width: 120,
      render: (_, record) => {
        const rate = record.submissionCount > 0
          ? ((record.acceptedCount / record.submissionCount) * 100).toFixed(1)
          : '0.0'
        return `${rate}%`
      },
    },
    {
      title: '通过/提交',
      key: 'stats',
      width: 120,
      render: (_, record) => `${record.acceptedCount} / ${record.submissionCount}`,
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
    <Card title="题库">
      <Space className="mb-4" wrap>
        <Search
          placeholder="搜索题目..."
          allowClear
          style={{ width: 250 }}
          prefix={<SearchOutlined />}
          onSearch={(value) => setQuery((prev) => ({ ...prev, search: value, page: 1 }))}
        />
        
        <Select
          placeholder="难度"
          allowClear
          style={{ width: 120 }}
          onChange={(value) => setQuery((prev) => ({ ...prev, difficulty: value, page: 1 }))}
          options={[
            { value: 1, label: '简单' },
            { value: 2, label: '中等' },
            { value: 3, label: '困难' },
          ]}
        />
        
        <Select
          placeholder="标签"
          allowClear
          style={{ width: 200 }}
          onChange={(value) => setQuery((prev) => ({ ...prev, tagId: value, page: 1 }))}
          options={tagsData?.data?.map((tag: TagType) => ({ value: tag.id, label: tag.name })) || []}
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
          showTotal: (total) => `共 ${total} 道题目`,
        }}
        onChange={handleTableChange}
      />
    </Card>
  )
}

export default ProblemsPage

import { useState } from 'react'
import { Table, Card, Select, Space, Button, Tag } from 'antd'
import { ReloadOutlined } from '@ant-design/icons'
import { Link } from 'react-router-dom'
import { useRequest } from 'ahooks'
import { contestService, ContestStatus, ContestType, type Contest, type ContestQuery } from '@/services'
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table'
import dayjs from 'dayjs'

const statusConfig: Record<number, { color: string; text: string }> = {
  [ContestStatus.Pending]: { color: 'default', text: '未开始' },
  [ContestStatus.Running]: { color: 'green', text: '进行中' },
  [ContestStatus.Frozen]: { color: 'blue', text: '已封榜' },
  [ContestStatus.Ended]: { color: 'red', text: '已结束' },
}

const ContestsPage = () => {
  const [query, setQuery] = useState<ContestQuery>({
    page: 1,
    pageSize: 20,
  })
  
  const { data, loading, refresh } = useRequest(
    () => contestService.getList(query),
    { refreshDeps: [query] }
  )
  
  const columns: ColumnsType<Contest> = [
    {
      title: '比赛名称',
      dataIndex: 'title',
      render: (text, record) => (
        <Link to={`/contests/${record.id}`} className="text-blue-600 hover:text-blue-800">
          {text}
        </Link>
      ),
    },
    {
      title: '状态',
      dataIndex: 'status',
      width: 100,
      render: (status) => {
        const config = statusConfig[status as number]
        return <Tag color={config?.color}>{config?.text || status}</Tag>
      },
    },
    {
      title: '类型',
      dataIndex: 'type',
      width: 100,
      render: (type) => {
        const typeMap: Record<number, string> = {
          [ContestType.ACM]: 'ACM',
          [ContestType.OI]: 'OI',
          [ContestType.IOI]: 'IOI',
          [ContestType.LeDuo]: 'LeDuo',
          [ContestType.Homework]: '作业',
        }
        return typeMap[type as number] || type
      },
    },
    {
      title: '开始时间',
      dataIndex: 'startTime',
      width: 180,
      render: (time) => dayjs(time).format('YYYY-MM-DD HH:mm'),
    },
    {
      title: '结束时间',
      dataIndex: 'endTime',
      width: 180,
      render: (time) => dayjs(time).format('YYYY-MM-DD HH:mm'),
    },
    {
      title: '参与人数',
      dataIndex: 'participantCount',
      width: 100,
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
    <Card title="比赛">
      <Space className="mb-4" wrap>
        <Select
          placeholder="状态"
          allowClear
          style={{ width: 120 }}
          onChange={(value) => setQuery((prev) => ({ ...prev, status: value, page: 1 }))}
          options={[
            { value: ContestStatus.Pending, label: '未开始' },
            { value: ContestStatus.Running, label: '进行中' },
            { value: ContestStatus.Frozen, label: '已封榜' },
            { value: ContestStatus.Ended, label: '已结束' },
          ]}
        />
        
        <Select
          placeholder="类型"
          allowClear
          style={{ width: 120 }}
          onChange={(value) => setQuery((prev) => ({ ...prev, type: value, page: 1 }))}
          options={[
            { value: ContestType.ACM, label: 'ACM' },
            { value: ContestType.OI, label: 'OI' },
            { value: ContestType.IOI, label: 'IOI' },
            { value: ContestType.LeDuo, label: 'LeDuo' },
            { value: ContestType.Homework, label: '作业' },
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
          showTotal: (total) => `共 ${total} 场比赛`,
        }}
        onChange={handleTableChange}
      />
    </Card>
  )
}

export default ContestsPage

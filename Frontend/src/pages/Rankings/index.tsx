import { Card, Table } from 'antd'
import { useRequest } from 'ahooks'
import api, { type PagedResult, type ApiResponse } from '@/services/api'
import type { ColumnsType } from 'antd/es/table'

interface RankingUser {
  rank: number
  userId: string
  username: string
  nickname?: string
  acceptedCount: number
  submissionCount: number
  score: number
}

const RankingsPage = () => {
  const { data, loading } = useRequest(
    (): Promise<ApiResponse<PagedResult<RankingUser>>> => api.get('/rankings')
  )
  
  const columns: ColumnsType<RankingUser> = [
    {
      title: '排名',
      dataIndex: 'rank',
      width: 80,
      render: (rank) => {
        if (rank === 1) return <span className="text-yellow-500 font-bold">🥇 1</span>
        if (rank === 2) return <span className="text-gray-400 font-bold">🥈 2</span>
        if (rank === 3) return <span className="text-orange-400 font-bold">🥉 3</span>
        return rank
      },
    },
    {
      title: '用户',
      dataIndex: 'username',
      render: (text, record) => record.nickname || text,
    },
    {
      title: '通过数',
      dataIndex: 'acceptedCount',
      width: 100,
    },
    {
      title: '提交数',
      dataIndex: 'submissionCount',
      width: 100,
    },
    {
      title: '通过率',
      key: 'acceptRate',
      width: 100,
      render: (_, record) => {
        const rate = record.submissionCount > 0
          ? ((record.acceptedCount / record.submissionCount) * 100).toFixed(1)
          : '0.0'
        return `${rate}%`
      },
    },
    {
      title: '积分',
      dataIndex: 'score',
      width: 100,
    },
  ]
  
  return (
    <Card title="排行榜">
      <Table
        columns={columns}
        dataSource={data?.data?.items || []}
        rowKey="userId"
        loading={loading}
        pagination={{
          pageSize: 50,
          showTotal: (total) => `共 ${total} 人`,
        }}
      />
    </Card>
  )
}

export default RankingsPage

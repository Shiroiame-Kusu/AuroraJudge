import { useParams, Link, useNavigate } from 'react-router-dom'
import { Card, Tabs, Table, Button, Tag, Descriptions, message } from 'antd'
import { useRequest } from 'ahooks'
import { contestService, ContestStatus, ContestType, type ContestDetail, type ContestProblem, type ContestRanking } from '@/services'
import { MarkdownRenderer, LoadingSpinner } from '@/components'
import { useAuthStore } from '@/stores'
import type { ColumnsType } from 'antd/es/table'
import dayjs from 'dayjs'

const ContestDetailPage = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { isAuthenticated } = useAuthStore()
  
  const { data, loading, refresh } = useRequest(
    () => contestService.getById(id!),
    { ready: !!id }
  )
  
  const { data: rankingData, loading: rankingLoading } = useRequest(
    () => contestService.getRanking(id!),
    { ready: !!id }
  )
  
  const contest = data?.data as ContestDetail | undefined
  const rankings = rankingData?.data as ContestRanking[] | undefined
  
  const handleRegister = async () => {
    if (!isAuthenticated) {
      message.warning('请先登录')
      navigate('/login')
      return
    }
    
    try {
      await contestService.register(id!)
      message.success('报名成功')
      refresh()
    } catch (error: any) {
      message.error(error.message || '报名失败')
    }
  }
  
  const problemColumns: ColumnsType<ContestProblem> = [
    {
      title: '序号',
      dataIndex: 'label',
      width: 80,
    },
    {
      title: '题目',
      dataIndex: 'title',
      render: (text, record) => (
        <Link to={`/problems/${record.problemId}?contestId=${id}`}>{text}</Link>
      ),
    },
    {
      title: '通过/提交',
      key: 'stats',
      width: 120,
      render: (_, record) => `${record.acceptedCount} / ${record.submissionCount}`,
    },
  ]
  
  const rankingColumns: ColumnsType<ContestRanking> = [
    {
      title: '排名',
      dataIndex: 'rank',
      width: 80,
    },
    {
      title: '用户',
      dataIndex: 'username',
      render: (text) => text,
    },
    {
      title: '总分',
      dataIndex: 'score',
      width: 100,
    },
    {
      title: '罚时',
      dataIndex: 'penalty',
      width: 100,
      render: (penalty) => `${penalty} min`,
    },
    {
      title: '解题数',
      dataIndex: 'solvedCount',
      width: 100,
    },
  ]
  
  if (loading) {
    return (
      <div className="flex items-center justify-center h-96">
        <LoadingSpinner size="large" />
      </div>
    )
  }
  
  if (!contest) {
    return <Card>比赛不存在</Card>
  }
  
  const statusConfig: Record<number, { color: string; text: string }> = {
    [ContestStatus.Pending]: { color: 'default', text: '未开始' },
    [ContestStatus.Running]: { color: 'green', text: '进行中' },
    [ContestStatus.Frozen]: { color: 'blue', text: '已封榜' },
    [ContestStatus.Ended]: { color: 'red', text: '已结束' },
  }
  
  const statusInfo = statusConfig[contest.status]

  const typeText: Record<number, string> = {
    [ContestType.ACM]: 'ACM',
    [ContestType.OI]: 'OI',
    [ContestType.IOI]: 'IOI',
    [ContestType.LeDuo]: 'LeDuo',
    [ContestType.Homework]: '作业',
  }

  return (
    <div className="space-y-4">
      <Card>
        <div className="flex justify-between items-start mb-4">
          <div>
            <h1 className="text-2xl font-bold mb-2">{contest.title}</h1>
            <Tag color={statusInfo?.color}>{statusInfo?.text}</Tag>
          </div>
          {!contest.isRegistered && contest.status !== ContestStatus.Ended && (
            <Button type="primary" onClick={handleRegister}>
              报名参加
            </Button>
          )}
          {contest.isRegistered && (
            <Tag color="blue">已报名</Tag>
          )}
        </div>
        
        <Descriptions bordered column={2} size="small">
          <Descriptions.Item label="开始时间">
            {dayjs(contest.startTime).format('YYYY-MM-DD HH:mm:ss')}
          </Descriptions.Item>
          <Descriptions.Item label="结束时间">
            {dayjs(contest.endTime).format('YYYY-MM-DD HH:mm:ss')}
          </Descriptions.Item>
          <Descriptions.Item label="类型">
            {typeText[contest.type] || contest.type}
          </Descriptions.Item>
          <Descriptions.Item label="参与人数">
            {contest.participantCount}
          </Descriptions.Item>
        </Descriptions>
      </Card>
      
      <Card>
        <Tabs
          items={[
            {
              key: 'problems',
              label: '题目列表',
              children: (
                <Table
                  columns={problemColumns}
                  dataSource={contest.problems || []}
                  rowKey="problemId"
                  pagination={false}
                />
              ),
            },
            {
              key: 'ranking',
              label: '排行榜',
              children: (
                <Table
                  columns={rankingColumns}
                  dataSource={rankings || []}
                  rowKey="userId"
                  loading={rankingLoading}
                  pagination={{ pageSize: 50 }}
                />
              ),
            },
            {
              key: 'description',
              label: '比赛说明',
              children: contest.description ? (
                <MarkdownRenderer content={contest.description} />
              ) : (
                <p className="text-gray-500">暂无说明</p>
              ),
            },
            {
              key: 'rules',
              label: '比赛规则',
              children: contest.rules ? (
                <MarkdownRenderer content={contest.rules} />
              ) : (
                <p className="text-gray-500">暂无规则</p>
              ),
            },
          ]}
        />
      </Card>
    </div>
  )
}

export default ContestDetailPage

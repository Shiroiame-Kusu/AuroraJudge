import { Card, Row, Col, Statistic, List, Typography, Tag, Space } from 'antd'
import { 
  CodeOutlined, 
  TrophyOutlined, 
  UserOutlined,
  FireOutlined,
} from '@ant-design/icons'
import { Link } from 'react-router-dom'
import { useRequest } from 'ahooks'
import { problemService, contestService, type Problem, type Contest } from '@/services'
import { DifficultyTag } from '@/components'

const { Title, Text } = Typography

const HomePage = () => {
  const { data: problems } = useRequest(() => 
    problemService.getList({ pageSize: 10 })
  )
  
  const { data: contests } = useRequest(() => 
    contestService.getList({ pageSize: 5, status: 1 })  // 1 = Running
  )
  
  return (
    <div className="space-y-6">
      {/* 欢迎区域 */}
      <Card>
        <Title level={2}>欢迎使用 Aurora Judge</Title>
        <Text type="secondary">
          Aurora Judge 是一个现代化的在线评测系统，支持多种编程语言，提供丰富的题目和比赛功能。
        </Text>
      </Card>
      
      {/* 统计卡片 */}
      <Row gutter={16}>
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="题目总数"
              value={problems?.data?.total || 0}
              prefix={<CodeOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="进行中的比赛"
              value={contests?.data?.total || 0}
              prefix={<TrophyOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="注册用户"
              value={1024}
              prefix={<UserOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="今日提交"
              value={256}
              prefix={<FireOutlined />}
            />
          </Card>
        </Col>
      </Row>
      
      <Row gutter={16}>
        {/* 最新题目 */}
        <Col xs={24} lg={12}>
          <Card 
            title="最新题目" 
            extra={<Link to="/problems">查看全部</Link>}
          >
            <List
              dataSource={problems?.data?.items || []}
              renderItem={(item: Problem) => (
                <List.Item>
                  <Space>
                    <Link to={`/problems/${item.id}`}>{item.title}</Link>
                    <DifficultyTag difficulty={item.difficulty} />
                  </Space>
                  <Text type="secondary">
                    通过率: {item.submissionCount > 0 
                      ? ((item.acceptedCount / item.submissionCount) * 100).toFixed(1) 
                      : 0}%
                  </Text>
                </List.Item>
              )}
            />
          </Card>
        </Col>
        
        {/* 进行中的比赛 */}
        <Col xs={24} lg={12}>
          <Card 
            title="进行中的比赛" 
            extra={<Link to="/contests">查看全部</Link>}
          >
            <List
              dataSource={contests?.data?.items || []}
              locale={{ emptyText: '暂无进行中的比赛' }}
              renderItem={(item: Contest) => (
                <List.Item>
                  <Link to={`/contests/${item.id}`}>{item.title}</Link>
                  <Tag color="green">进行中</Tag>
                </List.Item>
              )}
            />
          </Card>
        </Col>
      </Row>
      
      {/* 公告区域 */}
      <Card title="公告">
        <List>
          <List.Item>
            <List.Item.Meta
              title="系统上线公告"
              description="Aurora Judge 正式上线，欢迎大家使用！"
            />
            <Text type="secondary">2024-01-01</Text>
          </List.Item>
        </List>
      </Card>
    </div>
  )
}

export default HomePage

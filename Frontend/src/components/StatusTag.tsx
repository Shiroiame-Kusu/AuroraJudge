import { Tag } from 'antd'
import { JudgeStatus } from '@/services'

const statusConfig: Record<number, { color: string; text: string }> = {
  [JudgeStatus.Pending]: { color: 'default', text: '等待中' },
  [JudgeStatus.Judging]: { color: 'processing', text: '评测中' },
  [JudgeStatus.Compiling]: { color: 'processing', text: '编译中' },
  [JudgeStatus.Running]: { color: 'processing', text: '运行中' },
  [JudgeStatus.Accepted]: { color: 'success', text: '通过' },
  [JudgeStatus.WrongAnswer]: { color: 'error', text: '答案错误' },
  [JudgeStatus.TimeLimitExceeded]: { color: 'blue', text: '超时' },
  [JudgeStatus.MemoryLimitExceeded]: { color: 'purple', text: '超内存' },
  [JudgeStatus.OutputLimitExceeded]: { color: 'cyan', text: '输出超限' },
  [JudgeStatus.RuntimeError]: { color: 'orange', text: '运行错误' },
  [JudgeStatus.CompileError]: { color: 'gold', text: '编译错误' },
  [JudgeStatus.PresentationError]: { color: 'lime', text: '格式错误' },
  [JudgeStatus.SystemError]: { color: 'red', text: '系统错误' },
  [JudgeStatus.PartiallyAccepted]: { color: 'warning', text: '部分通过' },
  [JudgeStatus.Skipped]: { color: 'default', text: '跳过' },
}

interface StatusTagProps {
  status: JudgeStatus
}

const StatusTag = ({ status }: StatusTagProps) => {
  const config = statusConfig[status] || { color: 'default', text: String(status) }
  return <Tag color={config.color}>{config.text}</Tag>
}

export default StatusTag

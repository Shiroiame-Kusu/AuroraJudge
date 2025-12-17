import { Tag } from 'antd'

// 后端 ProblemDifficulty 枚举: Unrated=0, Easy=1, Medium=2, Hard=3, Expert=4
type Difficulty = number | 'Unrated' | 'Easy' | 'Medium' | 'Hard' | 'Expert'

const difficultyConfig: Record<number | string, { color: string; text: string }> = {
  0: { color: 'default', text: '未评级' },
  1: { color: 'green', text: '简单' },
  2: { color: 'orange', text: '中等' },
  3: { color: 'red', text: '困难' },
  4: { color: 'purple', text: '专家' },
  Unrated: { color: 'default', text: '未评级' },
  Easy: { color: 'green', text: '简单' },
  Medium: { color: 'orange', text: '中等' },
  Hard: { color: 'red', text: '困难' },
  Expert: { color: 'purple', text: '专家' },
}

interface DifficultyTagProps {
  difficulty: Difficulty
}

const DifficultyTag = ({ difficulty }: DifficultyTagProps) => {
  const config = difficultyConfig[difficulty] || { color: 'default', text: String(difficulty) }
  return <Tag color={config.color}>{config.text}</Tag>
}

export default DifficultyTag

import { Spin } from 'antd'
import { LoadingOutlined } from '@ant-design/icons'

export interface LoadingSpinnerProps {
  size?: 'small' | 'default' | 'large'
  tip?: string
  className?: string
}

const sizeToPx: Record<NonNullable<LoadingSpinnerProps['size']>, number> = {
  small: 14,
  default: 18,
  large: 28,
}

const LoadingSpinner = ({ size = 'default', tip, className }: LoadingSpinnerProps) => {
  const indicator = <LoadingOutlined style={{ fontSize: sizeToPx[size] }} spin />
  return <Spin size={size} tip={tip} indicator={indicator} className={className} />
}

export default LoadingSpinner

#!/bin/bash

# Aurora Judge - 本地开发启动脚本
# 用于快速启动整个开发环境

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}"
echo "    _                              _           _            "
echo "   / \  _   _ _ __ ___  _ __ __ _ | |_   _  __| | __ _  ___ "
echo "  / _ \| | | | '__/ _ \| '__/ _\` || | | | |/ _\` |/ _\` |/ _ \\"
echo " / ___ \ |_| | | | (_) | | | (_| || | |_| | (_| | (_| |  __/"
echo "/_/   \_\__,_|_|  \___/|_|  \__,_|/ |\__,_|\__,_|\__, |\___|"
echo "                                |__/            |___/       "
echo -e "${NC}"
echo -e "${GREEN}Aurora Judge - Online Judge System${NC}"
echo "=============================================="
echo ""

# 检查依赖
check_dependency() {
    if ! command -v $1 &> /dev/null; then
        echo -e "${RED}错误: 未找到 $1，请先安装${NC}"
        exit 1
    fi
}

check_dependency docker
check_dependency docker-compose
check_dependency dotnet
check_dependency node
check_dependency npm

echo -e "${GREEN}✓ 所有依赖检查通过${NC}"
echo ""

# 启动基础设施服务
start_infrastructure() {
    echo -e "${YELLOW}正在启动基础设施服务 (PostgreSQL, Redis, RabbitMQ, MinIO)...${NC}"
    docker-compose up -d postgres redis rabbitmq minio
    
    echo -e "${YELLOW}等待服务就绪...${NC}"
    sleep 10
    
    echo -e "${GREEN}✓ 基础设施服务已启动${NC}"
    echo "  - PostgreSQL: localhost:5432"
    echo "  - Redis: localhost:6379"  
    echo "  - RabbitMQ: localhost:5672 (管理界面: localhost:15672)"
    echo "  - MinIO: localhost:9000 (控制台: localhost:9001)"
    echo ""
}

# 启动后端
start_backend() {
    echo -e "${YELLOW}正在启动后端 API 服务...${NC}"
    cd Backend/AuroraJudge.Api
    dotnet run --urls "http://localhost:5000" &
    BACKEND_PID=$!
    cd ../..
    echo -e "${GREEN}✓ 后端 API 已启动 (PID: $BACKEND_PID)${NC}"
    echo "  - API: http://localhost:5000"
    echo "  - Swagger: http://localhost:5000/swagger"
    echo ""
}

# 启动判题机
start_judger() {
    echo -e "${YELLOW}正在启动判题机服务...${NC}"
    cd Judger/AuroraJudge.Judger
    dotnet run &
    JUDGER_PID=$!
    cd ../..
    echo -e "${GREEN}✓ 判题机已启动 (PID: $JUDGER_PID)${NC}"
    echo ""
}

# 启动前端
start_frontend() {
    echo -e "${YELLOW}正在安装前端依赖...${NC}"
    cd Frontend
    npm install
    
    echo -e "${YELLOW}正在启动前端开发服务器...${NC}"
    npm run dev &
    FRONTEND_PID=$!
    cd ..
    echo -e "${GREEN}✓ 前端已启动 (PID: $FRONTEND_PID)${NC}"
    echo "  - 前端: http://localhost:3000"
    echo ""
}

# 主流程
case "${1:-all}" in
    infra)
        start_infrastructure
        ;;
    backend)
        start_backend
        ;;
    judger)
        start_judger
        ;;
    frontend)
        start_frontend
        ;;
    all)
        start_infrastructure
        start_backend
        start_judger
        start_frontend
        
        echo "=============================================="
        echo -e "${GREEN}所有服务已启动！${NC}"
        echo ""
        echo "访问地址:"
        echo "  - 前端: http://localhost:3000"
        echo "  - API: http://localhost:5000"
        echo "  - Swagger: http://localhost:5000/swagger"
        echo "  - RabbitMQ 管理: http://localhost:15672 (guest/guest)"
        echo "  - MinIO 控制台: http://localhost:9001 (aurora/aurora123456)"
        echo ""
        echo -e "${YELLOW}默认管理员账号: admin / Admin@123456${NC}"
        echo ""
        echo "按 Ctrl+C 停止所有服务"
        
        # 等待用户中断
        trap "echo '正在停止服务...'; kill $BACKEND_PID $JUDGER_PID $FRONTEND_PID 2>/dev/null; docker-compose down; exit 0" INT
        wait
        ;;
    stop)
        echo -e "${YELLOW}正在停止所有服务...${NC}"
        docker-compose down
        pkill -f "AuroraJudge" 2>/dev/null || true
        echo -e "${GREEN}✓ 所有服务已停止${NC}"
        ;;
    *)
        echo "用法: $0 {all|infra|backend|judger|frontend|stop}"
        exit 1
        ;;
esac
